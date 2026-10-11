using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

// Licence/payment facts are included on the list row so a platform admin can see at a glance who
// is paid up, in grace, expired or suspended without opening each tenant. LastPaymentAtUtc is the
// latest SubscriptionRenewed billing event; null means no payment has ever been recorded (manual
// licences extended by an admin also leave it null).
public sealed record TenantDto(
    long Id,
    string Slug,
    string Name,
    bool IsActive,
    long? SubscriptionPlanId,
    bool IsProvisioned,
    LicenseStatus LicenseStatus,
    DateTime? TrialEndsAtUtc,
    DateTime? LicenseExpiresAtUtc,
    bool IsSuspended,
    DateTime? LastPaymentAtUtc);
