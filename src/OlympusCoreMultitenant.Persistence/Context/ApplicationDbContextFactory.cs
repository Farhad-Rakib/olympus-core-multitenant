using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OlympusCoreMultitenant.Infrastructure.MultiTenancy;

namespace OlympusCoreMultitenant.Persistence.Context;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var provider = "postgres".ToLowerInvariant();

        const string postgresConnection = "Host=localhost;Port=5432;Database=OlympusCoreMultitenantDb;Username=postgres;Password=postgres";

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
        return new ApplicationDbContext(optionsBuilder.Options, new CurrentTenantService(), new CurrentUserService());
    }
}
