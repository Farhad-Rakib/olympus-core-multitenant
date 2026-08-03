using OlympusCoreMultitenant.Application.Common.Dtos;

namespace OlympusCoreMultitenant.Application.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
