using ProjectNamePlaceholder.Application.Common.Dtos;

namespace ProjectNamePlaceholder.Application.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
