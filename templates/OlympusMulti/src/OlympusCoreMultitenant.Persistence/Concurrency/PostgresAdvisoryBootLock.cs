using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using OlympusCoreMultitenant.Application.Common.Interfaces;

namespace OlympusCoreMultitenant.Persistence.Concurrency;

// Serializes the boot-time migration/catalog-sync sequence across multiple concurrently-starting
// instances via a Postgres session-level advisory lock, so instance B waits out instance A's
// migration instead of racing it. Deliberately opens its own dedicated connection rather than
// reusing ApplicationDbContext's -- pg_advisory_lock is held by whichever connection acquired it,
// and this lock's only job is to serialize entry into the boot sequence; the guarded work itself
// runs on other (pooled) connections via the normal DbContext, which is fine.
public sealed class PostgresAdvisoryBootLock : IDistributedBootLock
{
    // Arbitrary fixed key. Postgres advisory locks share one 64-bit keyspace per database, so any
    // other code taking advisory locks in this database must pick a different key.
    private const long LockKey = 725_991_884_001;

    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresAdvisoryBootLock> _logger;

    public PostgresAdvisoryBootLock(IConfiguration configuration, ILogger<PostgresAdvisoryBootLock> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("PostgresConnection")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string: PostgresConnection.");

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        _logger.LogInformation("Waiting for the distributed boot lock (key {LockKey})...", LockKey);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT pg_advisory_lock(@key)";
            command.Parameters.AddWithValue("key", LockKey);
            command.CommandTimeout = 0; // block indefinitely -- another instance may still be migrating
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        _logger.LogInformation("Acquired the distributed boot lock.");

        return new Handle(connection, LockKey, _logger);
    }

    private sealed class Handle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly long _lockKey;
        private readonly ILogger _logger;

        public Handle(NpgsqlConnection connection, long lockKey, ILogger logger)
        {
            _connection = connection;
            _lockKey = lockKey;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = _connection.CreateCommand();
                command.CommandText = "SELECT pg_advisory_unlock(@key)";
                command.Parameters.AddWithValue("key", _lockKey);
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
