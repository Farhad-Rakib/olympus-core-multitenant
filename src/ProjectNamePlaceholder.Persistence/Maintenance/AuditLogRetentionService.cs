using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Maintenance;

// Deletes audit rows past their retention window. Lives in Persistence rather than Application
// because it deliberately bypasses the repository layer: this is a bulk maintenance operation, and
// loading rows into the change tracker just to delete them would defeat the point.
public sealed class AuditLogRetentionService : IAuditLogRetentionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuditLogOptions _options;

    public AuditLogRetentionService(ApplicationDbContext dbContext, IOptions<AuditLogOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        if (_options.RetentionDays <= 0)
        {
            return 0;
        }

        var cutoffUtc = DateTime.UtcNow.AddDays(-_options.RetentionDays);
        var batchSize = Math.Max(1, _options.CleanupBatchSize);
        var maxBatches = Math.Max(1, _options.MaxBatchesPerRun);

        var totalDeleted = 0;

        for (var batch = 0; batch < maxBatches; batch++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Ids are selected first, then deleted by key. ExecuteDeleteAsync does not accept Take()
            // across providers, and an unbounded delete is exactly what this is avoiding -- so the
            // batch boundary is established by the id query and the delete targets those keys only.
            // AuditLog has no global query filter, so this correctly spans every tenant.
            var ids = await _dbContext.AuditLogs
                .AsNoTracking()
                .Where(log => log.CreatedAt < cutoffUtc)
                .OrderBy(log => log.Id)
                .Select(log => log.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (ids.Count == 0)
            {
                break;
            }

            // Key-only stubs, not loaded entities: this issues DELETE ... WHERE Id = ... without
            // ever materialising the rows, which matters because Changes is an unbounded JSON blob
            // and loading a batch of them would defeat the purpose. ExecuteDeleteAsync would emit
            // one tidier statement, but it is unsupported by the InMemory provider the tests run
            // on, which would leave this logic untested.
            foreach (var id in ids)
            {
                // Reuse an already-tracked instance when there is one. Attaching a second instance
                // with the same key throws, and while the worker's scoped context is normally
                // clean, that is an assumption about the caller rather than something enforced here.
                var tracked = _dbContext.AuditLogs.Local.FirstOrDefault(log => log.Id == id);
                _dbContext.AuditLogs.Remove(tracked ?? new AuditLog { Id = id });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            totalDeleted += ids.Count;

            // A short batch means the backlog is drained; stop rather than issue an empty query.
            if (ids.Count < batchSize)
            {
                break;
            }
        }

        return totalDeleted;
    }
}
