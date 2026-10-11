namespace ProjectNamePlaceholder.Application.Security;

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string PermissionsRead = "permissions.read";
    public const string PermissionsCreate = "permissions.create";
    public const string PermissionsUpdate = "permissions.update";
    public const string PermissionsDelete = "permissions.delete";
    public const string UserRolesRead = "user-roles.read";
    public const string UserRolesCreate = "user-roles.create";
    public const string UserRolesUpdate = "user-roles.update";
    public const string UserRolesDelete = "user-roles.delete";
    public const string RolePermissionsRead = "role-permissions.read";
    public const string RolePermissionsCreate = "role-permissions.create";
    public const string RolePermissionsUpdate = "role-permissions.update";
    public const string RolePermissionsDelete = "role-permissions.delete";
    public const string SystemEndpointsRead = "system.endpoints.read";
    public const string SystemCacheRead = "system.cache.read";
    public const string SystemCacheFlush = "system.cache.flush";
    public const string MenusRead = "menus.read";
    public const string MenusCreate = "menus.create";
    public const string MenusUpdate = "menus.update";
    public const string MenusDelete = "menus.delete";
    public const string ReportsRead = "reports.read";
    public const string SiteSettingsRead = "site-settings.read";
    public const string SiteSettingsCreate = "site-settings.create";
    public const string SiteSettingsUpdate = "site-settings.update";
    public const string SiteSettingsDelete = "site-settings.delete";
    public const string TenantsManage = "tenants.manage";
    public const string SystemSettingsManage = "system-settings.manage";
    public const string SubscriptionPlansManage = "subscription-plans.manage";
    // Platform-wide: PlatformModule (ModuleKind.System), never delegable to a tenant. Grants the
    // cross-tenant view -- the caller may query any tenant, or omit the filter to see all of them.
    public const string AuditLogsRead = "audit-logs.read";

    // Tenant-scoped counterpart: CoreModule, so a tenant admin CAN hold it. The controller pins
    // the query to the ambient tenant for anyone holding only this one, so it can never widen into
    // a cross-tenant read.
    public const string AuditLogsReadOwn = "audit-logs.read-own";
    public const string FeatureFlagsRead = "feature-flags.read";
    public const string FeatureFlagsManage = "feature-flags.manage";

    // Tenant-scoped: lets a tenant's own admin start a hosted checkout or open the billing portal
    // for THEIR tenant. Deliberately in CoreModule (not PlatformModule) -- paying is the one thing
    // a locked-out tenant must be able to do without a platform admin's help.
    public const string BillingManage = "billing.manage";

    // Tenant-scoped UI translation overrides. Reading the bundle itself is anonymous (the login
    // page needs it); this gates the admin form.
    public const string TranslationsManage = "translations.manage";

    // "View as" a user of the same tenant for support. CoreModule so a tenant CAN delegate it, but
    // deliberately NOT auto-granted to the Admin role (RbacSeeder) -- it must be handed out on
    // purpose. Platform admins pass every policy and may impersonate across tenants.
    public const string UsersImpersonate = "users.impersonate";

    public static readonly string[] All =
    [
        UsersRead,
        UsersCreate,
        UsersUpdate,
        UsersDelete,
        RolesRead,
        RolesCreate,
        RolesUpdate,
        RolesDelete,
        PermissionsRead,
        PermissionsCreate,
        PermissionsUpdate,
        PermissionsDelete,
        UserRolesRead,
        UserRolesCreate,
        UserRolesUpdate,
        UserRolesDelete,
        RolePermissionsRead,
        RolePermissionsCreate,
        RolePermissionsUpdate,
        RolePermissionsDelete,
        MenusRead,
        MenusCreate,
        MenusUpdate,
        MenusDelete,
        ReportsRead,
        SiteSettingsRead,
        SiteSettingsCreate,
        SiteSettingsUpdate,
        SiteSettingsDelete,
        SystemEndpointsRead,
        SystemCacheRead,
        SystemCacheFlush,
        TenantsManage,
        SystemSettingsManage,
        SubscriptionPlansManage,
        AuditLogsRead,
        AuditLogsReadOwn,
        FeatureFlagsRead,
        FeatureFlagsManage,
        BillingManage,
        TranslationsManage,
        UsersImpersonate
    ];
}
