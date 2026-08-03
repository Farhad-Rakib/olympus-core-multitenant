using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ISubscriptionPlanRepository : IRepository<SubscriptionPlan>
{
    Task<SubscriptionPlan?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> HasAssignedTenantsAsync(long subscriptionPlanId, CancellationToken cancellationToken = default);
}
