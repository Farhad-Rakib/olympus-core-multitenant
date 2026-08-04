using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Persistence.Concurrency;

// SQL Server equivalent of PostgresAdvisoryBootLock: serializes the boot-time migration/catalog-sync
// sequence across multiple concurrently-starting instances via sp_getapplock, so instance B waits out
// instance A's migration instead of racing it. Deliberately opens its own dedicated connection rather
// than reusing ApplicationDbContext's -- an app lock is held by whichever session acquired it, and
// this lock's only job is to serialize entry into the boot sequence; the guarded work itself runs on
// other (pooled) connections via the normal DbContext, which is fine.
public sealed class SqlServerAdvisoryBootLock : IDistributedBootLock
{
    // Arbitrary fixed resource name. sp_getapplock resources are scoped per database, so any other
    // code taking app locks in this database must pick a different resource name.
    private const string LockResource = "Olympus_BootLock_725991884001";

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlServerAdvisoryBootLock> _logger;

    public SqlServerAdvisoryBootLock(IConfiguration configuration, ILogger<SqlServerAdvisoryBootLock> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("SqlServerConnection")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string: SqlServerConnection.");

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        _logger.LogInformation("Waiting for the distributed boot lock ({LockResource})...", LockResource);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "sp_getapplock";
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandTimeout = 0; // block indefinitely -- another instance may still be migrating
            command.Parameters.AddWithValue("@Resource", LockResource);
            command.Parameters.AddWithValue("@LockMode", "Exclusive");
            command.Parameters.AddWithValue("@LockOwner", "Session");
            command.Parameters.AddWithValue("@LockTimeout", -1);
            var returnValue = command.Parameters.Add("@ReturnValue", System.Data.SqlDbType.Int);
            returnValue.Direction = System.Data.ParameterDirection.ReturnValue;
            await command.ExecuteNonQueryAsync(cancellationToken);

            if ((int)returnValue.Value < 0)
            {
                throw new InvalidOperationException($"sp_getapplock failed to acquire '{LockResource}' (return code {returnValue.Value}).");
            }
        }
        _logger.LogInformation("Acquired the distributed boot lock.");

        return new Handle(connection, LockResource, _logger);
    }

    private sealed class Handle : IAsyncDisposable
    {
        private readonly SqlConnection _connection;
        private readonly string _lockResource;
        private readonly ILogger _logger;

        public Handle(SqlConnection connection, string lockResource, ILogger logger)
        {
            _connection = connection;
            _lockResource = lockResource;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = _connection.CreateCommand();
                command.CommandText = "sp_releaseapplock";
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Resource", _lockResource);
                command.Parameters.AddWithValue("@LockOwner", "Session");
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Released the distributed boot lock.");
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
