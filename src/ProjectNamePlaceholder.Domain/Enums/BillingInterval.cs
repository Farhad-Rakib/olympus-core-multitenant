namespace ProjectNamePlaceholder.Domain.Enums;

public enum BillingInterval
{
    // A plan that is not sold on a recurring cycle: internal, trial-only, or negotiated offline.
    // Price is expected to be zero here; the licence is managed by a platform admin.
    None,
    Monthly,
    Yearly
}
