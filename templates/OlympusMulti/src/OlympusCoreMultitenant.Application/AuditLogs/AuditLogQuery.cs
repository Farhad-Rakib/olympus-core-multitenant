using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.AuditLogs;

public sealed class AuditLogQuery
{
    public long? TenantId { get; init; }
    public string? EntityName { get; init; }
    public AuditAction? Action { get; init; }
    public long? UserId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
