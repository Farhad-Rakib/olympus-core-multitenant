using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IProcessedBillingEventRepository
{
    Task<bool> ExistsAsync(string provider, string externalEventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedBillingEvent processedEvent, CancellationToken cancellationToken = default);
    // Latest processed event of the given type per tenant, e.g. the last successful renewal.
    Task<IReadOnlyDictionary<long, DateTime>> GetLatestByTypeAsync(string eventType, CancellationToken cancellationToken = default);
}
