using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.ModuleSystem;

// Auto-entitled to every tenant, never independently toggleable -- owns users/roles/menus/site
// settings/feature-flags management. Mirrors what Modules.Core used to declare via the static
// ModuleDefinition array + PermissionModuleMap dictionary; those are now redundant with this class
// and DefaultMenuSeeder's hardcoded body, both retired in favor of ModuleHost discovery.
public sealed class CoreModule : IOlympusModule
{
    public string Key => Application.Security.Modules.Core;
    public string Name => "Authentication & Authorization";
    public string Description => "Users, roles, role-permissions, menus, site settings and feature flags management. Enabled for every tenant automatically and cannot be disabled.";
    public ModuleKind Kind => ModuleKind.Core;
    public IReadOnlyList<string> Dependencies => [];
    public IReadOnlyList<string> OptionalDependencies => [];

    public IReadOnlyList<PermissionDefinition> Permissions =>
    [
        new(Application.Security.Permissions.UsersRead, "View users"),
        new(Application.Security.Permissions.UsersCreate, "Create users"),
        new(Application.Security.Permissions.UsersUpdate, "Update users"),
        new(Application.Security.Permissions.UsersDelete, "Delete users"),
        new(Application.Security.Permissions.RolesRead, "View roles"),
        new(Application.Security.Permissions.RolesCreate, "Create roles"),
        new(Application.Security.Permissions.RolesUpdate, "Update roles"),
        new(Application.Security.Permissions.RolesDelete, "Delete roles"),
        new(Application.Security.Permissions.UserRolesRead, "View user-role assignments"),
        new(Application.Security.Permissions.UserRolesCreate, "Assign roles to users"),
        new(Application.Security.Permissions.UserRolesUpdate, "Update user-role assignments"),
        new(Application.Security.Permissions.UserRolesDelete, "Remove roles from users"),
        new(Application.Security.Permissions.RolePermissionsRead, "View role-permission assignments"),
        new(Application.Security.Permissions.RolePermissionsCreate, "Grant permissions to roles"),
        new(Application.Security.Permissions.RolePermissionsUpdate, "Update role-permission assignments"),
        new(Application.Security.Permissions.RolePermissionsDelete, "Revoke permissions from roles"),
        new(Application.Security.Permissions.MenusRead, "View menus"),
        new(Application.Security.Permissions.MenusCreate, "Create menus"),
        new(Application.Security.Permissions.MenusUpdate, "Update menus"),
        new(Application.Security.Permissions.MenusDelete, "Delete menus"),
        new(Application.Security.Permissions.ReportsRead, "View reports"),
        new(Application.Security.Permissions.SiteSettingsRead, "View site settings"),
        new(Application.Security.Permissions.SiteSettingsCreate, "Create site settings"),
        new(Application.Security.Permissions.SiteSettingsUpdate, "Update site settings"),
        new(Application.Security.Permissions.SiteSettingsDelete, "Delete site settings"),
        new(Application.Security.Permissions.FeatureFlagsRead, "View feature flags"),
        new(Application.Security.Permissions.FeatureFlagsManage, "Manage feature flags"),
    ];

    public IReadOnlyList<MenuDefinition> Menus =>
    [
        new("Dashboard", "/dashboard", "dashboard", null, null),
        new("Configuration", null, "settings", null, null),
        new("Users", "/users", "users", Application.Security.Permissions.UsersRead, "Configuration"),
        new("Roles", "/roles", "roles", Application.Security.Permissions.RolesRead, "Configuration"),
        new("Permissions", "/permissions", "permissions", Application.Security.Permissions.PermissionsRead, "Configuration"),
        new("User Roles", "/users/roles", "user-roles", Application.Security.Permissions.UserRolesRead, "Configuration"),
        new("Role Permissions", "/roles/permissions", "role-permissions", Application.Security.Permissions.RolePermissionsRead, "Configuration"),
        new("Reports", "/reports", "chart-bar", Application.Security.Permissions.ReportsRead, "Configuration"),
        new("Site Settings", "/site-settings", "settings", Application.Security.Permissions.SiteSettingsRead, "Configuration"),
        new("Feature Flags", "/feature-flags", "flag", Application.Security.Permissions.FeatureFlagsRead, "Configuration"),
    ];
}
