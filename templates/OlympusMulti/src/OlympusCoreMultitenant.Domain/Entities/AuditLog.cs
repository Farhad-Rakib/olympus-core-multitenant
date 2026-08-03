using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Domain.Entities;

// Deliberately not BaseEntity/ITenantEntity: this is an immutable record, and TenantId must stay
// nullable since platform-global entities (Tenant, Module, SubscriptionPlan) have no tenant.
public sealed class AuditLog
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public long? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string? Changes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
