using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Persistence.Context;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var provider = "postgres".ToLowerInvariant();

        const string postgresConnection = "Host=localhost;Port=5432;Database=ProjectNamePlaceholderDb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        if (provider == "postgres")
        {
            optionsBuilder.UseNpgsql(postgresConnection);
        }
        else
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
