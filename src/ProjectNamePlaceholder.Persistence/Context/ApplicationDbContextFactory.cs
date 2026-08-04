using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Persistence.Context;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Defaults to postgres so `dotnet ef` keeps working against this repo's actual database
        // unchanged; override to generate migrations for another provider without touching the
        // checked-in default, e.g. `OLYMPUS_DESIGN_TIME_PROVIDER=sqlserver dotnet ef migrations add ...`.
        var provider = (Environment.GetEnvironmentVariable("OLYMPUS_DESIGN_TIME_PROVIDER") ?? "postgres").ToLowerInvariant();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        //#if (database == "postgres")
        if (provider == "postgres")
        {
            const string postgresConnection = "Host=localhost;Port=5432;Database=ProjectNamePlaceholderDb;Username=postgres;Password=postgres";
            optionsBuilder.UseNpgsql(postgresConnection);
        }
        //#endif
        //#if (database == "sqlserver")
        if (provider == "sqlserver")
        {
            const string sqlServerConnection = "Server=localhost,1433;Database=ProjectNamePlaceholderDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True";
            optionsBuilder.UseSqlServer(sqlServerConnection);
        }
        //#endif
        if (!optionsBuilder.IsConfigured)
        {
            throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: postgres, sqlserver.");
        }

        // Design-time only: migrations never execute queries, so an unresolved tenant/user context is fine here.
        // Local no-op stubs, not the real Infrastructure implementations -- Persistence must not reference
        // Infrastructure at runtime, and design-time tooling is the only caller of this factory.
        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeCurrentTenantService(), new DesignTimeCurrentUserService());
    }

    private sealed class DesignTimeCurrentTenantService : ICurrentTenantService
    {
        public long? TenantId => null;
        public string? TenantSlug => null;
        public void Set(long tenantId, string tenantSlug) { }
        public IDisposable BeginScope(long tenantId, string tenantSlug) => NullScope.Instance;
        public void Clear() { }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public long? UserId => null;
        public void Set(long userId) { }
    }
}
