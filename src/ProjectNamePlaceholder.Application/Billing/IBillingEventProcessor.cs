namespace ProjectNamePlaceholder.Application.Billing;

public interface IBillingEventProcessor
{
    Task<BillingEventResult> ProcessAsync(BillingEvent billingEvent, CancellationToken cancellationToken = default);
}

// The Application layer takes no logging dependency (enforced by LayeringTests), so the outcome is
// returned for the Api layer to log rather than written to a logger here.
public enum BillingEventResult
{
    Applied,

    // Already processed. A duplicate delivery is success, not an error -- providers retry routinely.
    DuplicateIgnored,

    // Authentic event naming a tenant that does not exist. Retrying will never fix it, so the
    // caller must still answer 2xx.
    UnknownTenant,

    UnknownPlan,

    // Payment failed and the licence was deliberately left to lapse into the grace period.
    PaymentFailureNoted
}
