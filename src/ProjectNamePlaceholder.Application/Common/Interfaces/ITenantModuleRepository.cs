using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ITenantModuleRepository
{
    Task<IReadOnlyList<long>> GetEntitledModuleIdsAsync(CancellationToken cancellationToken = default);
    Task<TenantModule?> GetAsync(long moduleId, CancellationToken cancellationToken = default);
    Task AddAsync(TenantModule tenantModule, CancellationToken cancellationToken = default);
    void Remove(TenantModule tenantModule);
}
