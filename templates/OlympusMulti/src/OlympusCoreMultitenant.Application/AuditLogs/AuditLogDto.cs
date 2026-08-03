using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.AuditLogs;

public sealed record AuditLogDto(
    long Id,
    long? TenantId,
    string? TenantName,
    long? UserId,
    string? UserName,
    string EntityName,
    long? EntityId,
    AuditAction Action,
    string? Changes,
    DateTime CreatedAt);
