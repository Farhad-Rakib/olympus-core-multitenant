using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.AuditLogs;

public sealed class AuditLogQuery
{
    // Caller-supplied filter, freely chosen -- only meaningful for a platform admin doing a
    // cross-tenant query. Narrows the result set; it can never widen past EnforcedTenantId.
    public long? TenantId { get; init; }

    // Set by the API layer for any caller who is NOT a platform admin. The repository applies it
    // unconditionally and ignores TenantId when it is present, so a tenant-scoped caller cannot
    // reach another tenant's rows by passing (or omitting) the tenantId query parameter. AuditLog
    // is deliberately not an ITenantEntity -- it has no global query filter -- so this is the only
    // thing standing between a tenant admin and the whole table. Keep the enforcement here in the
    // repository rather than only at the controller.
    public long? EnforcedTenantId { get; init; }
    public string? EntityName { get; init; }
    public AuditAction? Action { get; init; }
    public long? UserId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
