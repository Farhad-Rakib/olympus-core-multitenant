using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.UnitTests.TestSupport;

public sealed class FakeTenantRepository : IRepository<Tenant>
{
    private readonly List<Tenant> _tenants = new();
    private long _nextId = 1;

    public Task<Tenant> AddAsync(Tenant entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        _tenants.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Tenant?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tenants.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Tenant>>(_tenants.ToList());

    public void Update(Tenant entity)
    {
        // No-op: the fake shares the same in-memory instance the caller already mutated.
    }

    public void Delete(Tenant entity)
    {
        _tenants.RemoveAll(t => t.Id == entity.Id);
    }
}
