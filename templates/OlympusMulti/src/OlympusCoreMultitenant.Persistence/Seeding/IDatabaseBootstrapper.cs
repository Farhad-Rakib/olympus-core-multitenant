namespace OlympusCoreMultitenant.Persistence.Seeding;

public interface IDatabaseBootstrapper
{
    // Applies pending EF migrations only. Must run before ModuleSyncService/PermissionSyncService
    // (which need the schema to exist) and before SeedDefaultTenantAsync (which needs the module
    // and permission catalogs already synced -- RbacSeeder now grants Admin only permissions from
    // entitled, non-null-ModuleId modules).
    Task MigrateAsync(CancellationToken cancellationToken = default);

    // Seeds global system settings and provisions the "default" tenant. Must run after
    // ModuleSyncService/PermissionSyncService have populated the Module/Permission catalogs.
    Task SeedDefaultTenantAsync(CancellationToken cancellationToken = default);
}
