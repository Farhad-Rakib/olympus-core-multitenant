namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface ISubscriptionPlanModuleRepository
{
    Task<IReadOnlyList<long>> GetModuleIdsForPlanAsync(long subscriptionPlanId, CancellationToken cancellationToken = default);
    Task ReplaceModulesAsync(long subscriptionPlanId, IReadOnlyList<long> moduleIds, CancellationToken cancellationToken = default);
}
