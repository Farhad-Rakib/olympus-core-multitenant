namespace OlympusCoreMultitenant.Application.Security;

// Just the module key constants -- everything else this class used to hold (the ModuleDefinition
// catalog array, and the permission-to-module map) is now declared by CoreModule/PlatformModule
// themselves and discovered via ModuleHost (see Application.ModuleSystem). These two constants
// stay here since they're referenced by call sites (e.g. TenantProvisioningService's default
// module-entitlement key, TenantModule seeding) that have nothing to do with the module catalog
// itself, just need a stable key literal.
public static class Modules
{
    // Auto-entitled to every tenant, never independently toggleable. Owns the permissions
    // every tenant needs from day one (users/roles/menus/site settings management).
    public const string Core = "core";

    // Superadmin-only platform operations. Never entitled or visible to any tenant, regardless
    // of module entitlement -- these permissions can never be delegated to a tenant admin.
    public const string Platform = "platform";
}
