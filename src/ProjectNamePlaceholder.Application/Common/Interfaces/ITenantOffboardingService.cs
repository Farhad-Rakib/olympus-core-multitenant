using ProjectNamePlaceholder.Application.Tenants.Dtos;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ITenantOffboardingService
{
    Task<TenantExportDto> ExportAsync(Tenant tenant, CancellationToken cancellationToken = default);

    // Irreversibly deletes every row belonging to this tenant (users, roles, role-permissions,
    // menus, site settings, feature flags, module entitlements, domains, registration tokens) and
    // finally the tenant row itself. Audit logs referencing this tenant are deliberately left in
    // place -- see TenantExportDto's doc comment. Callers must have already verified the tenant is
    // eligible (disabled, past its cooling-off period) -- see ITenantService.PurgeAsync.
    Task PurgeAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
