using OlympusCoreMultitenant.Application.Subscriptions.Dtos;

namespace OlympusCoreMultitenant.Application.Subscriptions;

public interface ISubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlanDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequestDto request, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanDto> UpdateAsync(long id, UpdateSubscriptionPlanRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
