using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.UnitTests.TestSupport;

public sealed class FakeSubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly List<SubscriptionPlan> _plans = new();
    private long _nextId = 1;

    public Task<SubscriptionPlan> AddAsync(SubscriptionPlan entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        _plans.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<SubscriptionPlan?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_plans.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<SubscriptionPlan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SubscriptionPlan>>(_plans.ToList());

    public Task<SubscriptionPlan?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_plans.FirstOrDefault(p => p.Key == key));

    public Task<bool> HasAssignedTenantsAsync(long subscriptionPlanId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public void Update(SubscriptionPlan entity)
    {
    }

    public void Delete(SubscriptionPlan entity)
    {
        _plans.RemoveAll(p => p.Id == entity.Id);
    }
}
