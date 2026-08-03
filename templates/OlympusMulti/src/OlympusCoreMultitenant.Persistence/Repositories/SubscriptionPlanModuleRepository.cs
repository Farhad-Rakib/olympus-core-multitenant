using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class SubscriptionPlanModuleRepository : ISubscriptionPlanModuleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SubscriptionPlanModuleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<long>> GetModuleIdsForPlanAsync(long subscriptionPlanId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPlanModules
            .AsNoTracking()
            .Where(planModule => planModule.SubscriptionPlanId == subscriptionPlanId)
            .Select(planModule => planModule.ModuleId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceModulesAsync(long subscriptionPlanId, IReadOnlyList<long> moduleIds, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SubscriptionPlanModules
            .Where(planModule => planModule.SubscriptionPlanId == subscriptionPlanId)
            .ToListAsync(cancellationToken);

        _dbContext.SubscriptionPlanModules.RemoveRange(existing);

        foreach (var moduleId in moduleIds.Distinct())
        {
            await _dbContext.SubscriptionPlanModules.AddAsync(
                new SubscriptionPlanModule { SubscriptionPlanId = subscriptionPlanId, ModuleId = moduleId },
                cancellationToken);
        }
    }
}
