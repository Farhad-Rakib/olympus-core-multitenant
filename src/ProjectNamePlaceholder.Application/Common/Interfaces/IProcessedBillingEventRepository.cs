using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IProcessedBillingEventRepository
{
    Task<bool> ExistsAsync(string provider, string externalEventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedBillingEvent processedEvent, CancellationToken cancellationToken = default);
}
