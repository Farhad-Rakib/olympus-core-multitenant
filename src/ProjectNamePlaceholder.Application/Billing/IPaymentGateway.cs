namespace ProjectNamePlaceholder.Application.Billing;

// The seam that keeps the provider decision reversible. Everything above this interface deals in
// BillingEvent, never in a provider's own payload shape.
public interface IPaymentGateway
{
    // Matches PaymentOptions.Provider; used to select the active implementation at startup.
    string Provider { get; }

    // Verifies the signature and translates the provider's payload into a BillingEvent. Returns
    // null when the payload is authentic but irrelevant (providers send many event types we do not
    // act on) -- that is a 200, not an error, or the provider will retry forever.
    // Throws when the signature does not verify.
    Task<BillingEvent?> ParseWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);

    // Hosted checkout: we deliberately do not build a payment form, which would drag card data --
    // and PCI scope -- through this application.
    Task<string> CreateCheckoutUrlAsync(long tenantId, string planKey, CancellationToken cancellationToken = default);

    // Hosted customer portal: payment-method updates, invoice history and cancellation, none of
    // which we have to build.
    Task<string> CreateBillingPortalUrlAsync(long tenantId, CancellationToken cancellationToken = default);
}
