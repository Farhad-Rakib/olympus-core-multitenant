using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjectNamePlaceholder.Application.Auth;
using ProjectNamePlaceholder.Application.AuditLogs;
using ProjectNamePlaceholder.Application.Billing;
using ProjectNamePlaceholder.Application.FeatureFlags;
using ProjectNamePlaceholder.Application.Integration;
using ProjectNamePlaceholder.Application.Licensing;
using ProjectNamePlaceholder.Application.Menu;
using ProjectNamePlaceholder.Application.Permissions;
using ProjectNamePlaceholder.Application.Roles;
using ProjectNamePlaceholder.Application.SiteSettings;
using ProjectNamePlaceholder.Application.Subscriptions;
using ProjectNamePlaceholder.Application.SystemSettings;
using ProjectNamePlaceholder.Application.Tenants;
using ProjectNamePlaceholder.Application.Translations;
using ProjectNamePlaceholder.Application.Tenants.Events;
using ProjectNamePlaceholder.Application.Users;
using ProjectNamePlaceholder.Application.Users.EventHandlers;

namespace ProjectNamePlaceholder.Application;

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
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<ITenantBillingService, TenantBillingService>();
        services.AddScoped<ITranslationService, TranslationService>();
        services.AddScoped<IImpersonationService, ImpersonationService>();
        services.AddScoped<IIntegrationEventHandler<TenantApprovedIntegrationEvent>, CreateAdminUserOnTenantApprovedHandler>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
