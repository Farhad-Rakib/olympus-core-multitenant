using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ITenantFeatureFlagRepository
{
    Task<TenantFeatureFlag> AddAsync(TenantFeatureFlag entity, CancellationToken cancellationToken = default);
    Task<TenantFeatureFlag?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<TenantFeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantFeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);
    void Update(TenantFeatureFlag entity);
    void Delete(TenantFeatureFlag entity);
}
