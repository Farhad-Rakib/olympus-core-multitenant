using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Persistence.Context;
using ProjectNamePlaceholder.Persistence.Repositories;
using ProjectNamePlaceholder.Persistence.Seeding;
using ProjectNamePlaceholder.Application.Menu;

namespace ProjectNamePlaceholder.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var provider = GetProvider(configuration);
            //#if (database == "postgres")
            if (provider == "postgres")
            {
                options.UseNpgsql(GetConnectionString(configuration, "PostgresConnection"));
            }
            //#endif
            //#if (database == "sqlserver")
            if (provider == "sqlserver")
            {
                options.UseSqlServer(GetConnectionString(configuration, "SqlServerConnection"));
            }
            //#endif
        });

        services.AddScoped<IAuditLogRetentionService, ProjectNamePlaceholder.Persistence.Maintenance.AuditLogRetentionService>();
        services.AddScoped<IProcessedBillingEventRepository, ProcessedBillingEventRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<ITenantFeatureFlagRepository, TenantFeatureFlagRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantRegistrationTokenRepository, TenantRegistrationTokenRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ITenantModuleRepository, TenantModuleRepository>();
        services.AddScoped<ITenantDomainRepository, TenantDomainRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ISubscriptionPlanModuleRepository, SubscriptionPlanModuleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IRbacSeeder, RbacSeeder>();
        services.AddScoped<IDatabaseBootstrapper, EfCoreDatabaseBootstrapper>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<Application.Common.Interfaces.ITenantProvisioningService, TenantProvisioningService>();
        //#if (database == "postgres")
        if (GetProvider(configuration) == "postgres")
        {
            services.AddScoped<Application.Common.Interfaces.IDistributedBootLock, Concurrency.PostgresAdvisoryBootLock>();
        }
        //#endif
        //#if (database == "sqlserver")
        if (GetProvider(configuration) == "sqlserver")
        {
            services.AddScoped<Application.Common.Interfaces.IDistributedBootLock, Concurrency.SqlServerAdvisoryBootLock>();
        }
        //#endif
        services.AddScoped<Application.Common.Interfaces.ITenantOffboardingService, Offboarding.TenantOffboardingService>();

        return services;
    }

    // Falls back to postgres for anything unrecognized rather than throwing: a `dotnet new`-generated
    // solution always has this resolved to a real value by the `database` template parameter, so the
    // fallback only matters when running the raw, un-generated template repo directly (where
    // Database:Provider's value is a template substitution token until `dotnet new` resolves it).
    private static string GetProvider(IConfiguration configuration)
    {
        var provider = (configuration["Database:Provider"] ?? "postgres").ToLowerInvariant();
        return provider is "postgres" or "sqlserver" ? provider : "postgres";
    }

    // Treats blank as missing, not just null: the environment-specific appsettings files ship these
    // keys empty on purpose (the real value comes from an environment variable), and an empty string
    // would otherwise sail past a null check and surface as an opaque provider-level parse error.
    private static string GetConnectionString(IConfiguration configuration, string key)
    {
        var connectionString = configuration.GetConnectionString(key);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Missing connection string: {key}. Set it in appsettings.json, or supply the " +
                $"ConnectionStrings__{key} environment variable.");
        }

        return connectionString;
    }
}
