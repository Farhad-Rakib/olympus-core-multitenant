using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.UnitTests.TestSupport;

public sealed class FakeTenantFeatureFlagRepository : ITenantFeatureFlagRepository
{
    private readonly List<TenantFeatureFlag> _flags = new();
    private long _nextId = 1;

    public Task<TenantFeatureFlag> AddAsync(TenantFeatureFlag entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        _flags.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<TenantFeatureFlag?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_flags.FirstOrDefault(f => f.Id == id));

    public Task<TenantFeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_flags.FirstOrDefault(f => f.Key == key));

    public Task<IReadOnlyList<TenantFeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TenantFeatureFlag>>(_flags.ToList());

    public void Update(TenantFeatureFlag entity)
    {
        // No-op: the fake shares the same in-memory instance the caller already mutated.
    }

    public void Delete(TenantFeatureFlag entity)
    {
        _flags.RemoveAll(f => f.Id == entity.Id);
    }
}
