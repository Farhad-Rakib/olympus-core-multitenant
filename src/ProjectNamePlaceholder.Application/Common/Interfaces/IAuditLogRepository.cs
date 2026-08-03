using ProjectNamePlaceholder.Application.AuditLogs;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
