using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.ModuleSystem;

// Superadmin-only platform operations: the global permission catalog, tenant management, system
// settings, subscription plans, licensing and cache administration. Never entitled or visible to
// any tenant, regardless of module entitlement -- these permissions can never be delegated to a
// tenant admin. Mirrors what Modules.Platform used to declare statically; see CoreModule for the
// broader context on why this replaces that array.
public sealed class PlatformModule : IOlympusModule
{
    public string Key => Application.Security.Modules.Platform;
    public string Name => "Platform Administration";
    public string Description => "Global permission catalog, tenant management, system settings, subscription plans and cache administration. Never entitled to any tenant.";
    public ModuleKind Kind => ModuleKind.System;
    public IReadOnlyList<string> Dependencies => [];
    public IReadOnlyList<string> OptionalDependencies => [];

    public IReadOnlyList<PermissionDefinition> Permissions =>
    [
        new(Application.Security.Permissions.PermissionsRead, "View the permission catalog"),
        new(Application.Security.Permissions.PermissionsCreate, "Create permissions"),
        new(Application.Security.Permissions.PermissionsUpdate, "Update permissions"),
        new(Application.Security.Permissions.PermissionsDelete, "Delete permissions"),
        new(Application.Security.Permissions.SystemEndpointsRead, "View registered API endpoints"),
        new(Application.Security.Permissions.SystemCacheRead, "View cache diagnostics"),
        new(Application.Security.Permissions.SystemCacheFlush, "Flush the distributed cache"),
        new(Application.Security.Permissions.TenantsManage, "Manage tenants"),
        new(Application.Security.Permissions.SystemSettingsManage, "Manage global system settings"),
        new(Application.Security.Permissions.SubscriptionPlansManage, "Manage subscription plans"),
        new(Application.Security.Permissions.AuditLogsRead, "View audit logs"),
    ];

    public IReadOnlyList<MenuDefinition> Menus =>
    [
        new("Platform", null, "server", null, null),
        new("Tenants", "/tenants", "Building2", Application.Security.Permissions.TenantsManage, "Platform"),
        new("Subscription Plans", "/subscription-plans", "Package", Application.Security.Permissions.SubscriptionPlansManage, "Platform"),
        new("System Settings", "/system-settings", "sliders", Application.Security.Permissions.SystemSettingsManage, "Platform"),
    ];
}
