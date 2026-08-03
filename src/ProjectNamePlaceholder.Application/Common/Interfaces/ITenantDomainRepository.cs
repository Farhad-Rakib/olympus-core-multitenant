using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ITenantDomainRepository
{
    Task<TenantDomain> AddAsync(TenantDomain entity, CancellationToken cancellationToken = default);
    Task<TenantDomain?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<TenantDomain?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantDomain>> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default);
    void Delete(TenantDomain entity);
}
