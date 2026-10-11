namespace ProjectNamePlaceholder.Application.Billing;

// Provider-neutral shape every gateway normalises into. Deliberately small: these are the only
// facts the licence layer needs to decide access.
public sealed record BillingEvent(
    // Provider's own event id. Stored to make webhook processing idempotent -- every provider
    // retries on non-2xx and may deliver the same event more than once even on success.
    string ExternalEventId,
    BillingEventType Type,
    long TenantId,
    // Paid-through date. Drives Tenant.LicenseExpiresAtUtc, so non-payment needs no explicit
    // "revoke" event: the licence simply stops being extended and expires on its own.
    DateTime? PeriodEndUtc,
    string? PlanKey);

public enum BillingEventType
{
    // Payment settled: extend the licence and lift any suspension.
    SubscriptionRenewed,

    // Payment failed. Deliberately NOT an immediate lockout -- the existing grace period is the
    // dunning window while the provider retries the card.
    PaymentFailed,

    // Terminal: cancelled, or dunning exhausted. Suspend.
    SubscriptionCancelled,

    // Upgrade/downgrade: re-point the tenant at a different plan.
    PlanChanged
}
