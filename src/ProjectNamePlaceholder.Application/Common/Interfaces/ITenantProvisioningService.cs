using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ITenantProvisioningService
{
    Task ProvisionAsync(Tenant tenant, IReadOnlyList<string>? moduleKeys = null, CancellationToken cancellationToken = default);

    // Re-runs the idempotent, upsert/add-only catalog seeders (roles, permissions, role-permissions,
    // site settings, menus) against an already-provisioned tenant, without touching its module
    // entitlements. For tenants that provisioned before a later RbacSeeder/DefaultMenuSeeder change
    // added new defaults -- see ITenantService.ReprovisionCatalogAsync/ReprovisionAllCatalogsAsync.
    Task ResyncCatalogAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
