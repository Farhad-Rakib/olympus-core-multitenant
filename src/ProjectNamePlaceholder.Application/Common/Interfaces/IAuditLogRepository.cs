using ProjectNamePlaceholder.Application.AuditLogs;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken = default);

    // For explicit "Accessed"-style rows (impersonation, exports) that SaveChanges auditing cannot
    // see because nothing was written. Ordinary writes are captured automatically; do not add rows
    // for those here or they will be double-logged.
    Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default);
}
