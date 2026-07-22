using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class SubscriptionPlanRepository : BaseRepository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<SubscriptionPlan?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DbContext.SubscriptionPlans
            .FirstOrDefaultAsync(plan => plan.Key == key, cancellationToken);
    }

    // Tenant carries the FK regardless of which tenant is ambient, so this must see every tenant.
    public async Task<bool> HasAssignedTenantsAsync(long subscriptionPlanId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Tenants
            .AnyAsync(tenant => tenant.SubscriptionPlanId == subscriptionPlanId, cancellationToken);
    }
}
