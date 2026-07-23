using OlympusCoreMultitenant.Application.AuditLogs;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
