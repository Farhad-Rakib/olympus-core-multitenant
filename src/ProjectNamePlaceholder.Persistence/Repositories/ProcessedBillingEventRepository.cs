using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class ProcessedBillingEventRepository : IProcessedBillingEventRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProcessedBillingEventRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string provider, string externalEventId, CancellationToken cancellationToken = default)
        => _dbContext.ProcessedBillingEvents
            .AsNoTracking()
            .AnyAsync(e => e.Provider == provider && e.ExternalEventId == externalEventId, cancellationToken);

    public async Task AddAsync(ProcessedBillingEvent processedEvent, CancellationToken cancellationToken = default)
        => await _dbContext.ProcessedBillingEvents.AddAsync(processedEvent, cancellationToken);
}
