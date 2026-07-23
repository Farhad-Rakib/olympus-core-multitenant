using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.Security;

public sealed record ModuleDefinition(string Key, string Name, string Description, ModuleKind Kind);

public static class Modules
{
    // Auto-entitled to every tenant, never independently toggleable. Owns the permissions
    // every tenant needs from day one (users/roles/menus/site settings management).
    public const string Core = "core";

    // Superadmin-only platform operations. Never entitled or visible to any tenant, regardless
    // of module entitlement -- these permissions can never be delegated to a tenant admin.
    public const string Platform = "platform";

    public static readonly ModuleDefinition[] All =
    [
        new ModuleDefinition(
            Core,
            "Authentication & Authorization",
            "Users, roles, role-permissions, menus and site settings management. Enabled for every tenant automatically and cannot be disabled.",
            ModuleKind.Core),
        new ModuleDefinition(
            Platform,
            "Platform Administration",
            "Superadmin-only platform operations: the global permission catalog, tenant management, system settings and cache administration. Never entitled to any tenant.",
            ModuleKind.System)
    ];

    // Maps every existing Permissions.cs constant to the module that owns it. When a new
    // Business module is added later (its own controllers/services/permissions), add its
    // permissions here mapped to the new module's key -- this is the single source of truth
    // PermissionSyncService uses to backfill Permission.ModuleId.
    public static readonly IReadOnlyDictionary<string, string> PermissionModuleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [Permissions.UsersRead] = Core,
        [Permissions.UsersCreate] = Core,
        [Permissions.UsersUpdate] = Core,
        [Permissions.UsersDelete] = Core,
        [Permissions.RolesRead] = Core,
        [Permissions.RolesCreate] = Core,
        [Permissions.RolesUpdate] = Core,
        [Permissions.RolesDelete] = Core,
        [Permissions.UserRolesRead] = Core,
        [Permissions.UserRolesCreate] = Core,
        [Permissions.UserRolesUpdate] = Core,
        [Permissions.UserRolesDelete] = Core,
        [Permissions.RolePermissionsRead] = Core,
        [Permissions.RolePermissionsCreate] = Core,
        [Permissions.RolePermissionsUpdate] = Core,
        [Permissions.RolePermissionsDelete] = Core,
        [Permissions.MenusRead] = Core,
        [Permissions.MenusCreate] = Core,
        [Permissions.MenusUpdate] = Core,
        [Permissions.MenusDelete] = Core,
        [Permissions.ReportsRead] = Core,
        [Permissions.SiteSettingsRead] = Core,
        [Permissions.SiteSettingsCreate] = Core,
        [Permissions.SiteSettingsUpdate] = Core,
        [Permissions.SiteSettingsDelete] = Core,

        [Permissions.PermissionsRead] = Platform,
        [Permissions.PermissionsCreate] = Platform,
        [Permissions.PermissionsUpdate] = Platform,
        [Permissions.PermissionsDelete] = Platform,
        [Permissions.SystemEndpointsRead] = Platform,
        [Permissions.SystemCacheRead] = Platform,
        [Permissions.SystemCacheFlush] = Platform,
        [Permissions.TenantsManage] = Platform,
        [Permissions.SystemSettingsManage] = Platform,
        [Permissions.SubscriptionPlansManage] = Platform,
        [Permissions.AuditLogsRead] = Platform
    };
}
