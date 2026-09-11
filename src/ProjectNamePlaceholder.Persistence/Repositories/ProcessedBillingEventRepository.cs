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

    public async Task<IReadOnlyDictionary<long, DateTime>> GetLatestByTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.ProcessedBillingEvents
            .AsNoTracking()
            .Where(e => e.EventType == eventType)
            .GroupBy(e => e.TenantId)
            .Select(g => new { TenantId = g.Key, At = g.Max(e => e.ProcessedAtUtc) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.TenantId, r => r.At);
    }
}
