using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Persistence.Context;
using OlympusCoreMultitenant.Persistence.Repositories;
using OlympusCoreMultitenant.Persistence.Seeding;
using OlympusCoreMultitenant.Application.Menu;

namespace OlympusCoreMultitenant.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var provider = GetProvider(configuration);
            if (provider == "postgres")
            {
                options.UseNpgsql(GetConnectionString(configuration, "PostgresConnection"));
            }
            else
            {
                throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: postgres, sqlserver.");
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ITenantModuleRepository, TenantModuleRepository>();
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

        return services;
    }

    private static string GetProvider(IConfiguration configuration)
    {
        var provider = (configuration["Database:Provider"] ?? "postgres").ToLowerInvariant();
        if (provider is not ("postgres" or "sqlserver"))
        {
            throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: postgres, sqlserver.");
        }

        return provider;
    }

    private static string GetConnectionString(IConfiguration configuration, string key)
    {
        return configuration.GetConnectionString(key)
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException($"Missing connection string: {key}.");
    }
}
