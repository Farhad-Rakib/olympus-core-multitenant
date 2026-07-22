using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OlympusCoreMultitenant.Application.Auth;
using OlympusCoreMultitenant.Application.Menu;
using OlympusCoreMultitenant.Application.Permissions;
using OlympusCoreMultitenant.Application.Roles;
using OlympusCoreMultitenant.Application.SiteSettings;
using OlympusCoreMultitenant.Application.Subscriptions;
using OlympusCoreMultitenant.Application.SystemSettings;
using OlympusCoreMultitenant.Application.Tenants;
using OlympusCoreMultitenant.Application.Users;

namespace OlympusCoreMultitenant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ISiteSettingService, SiteSettingService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
