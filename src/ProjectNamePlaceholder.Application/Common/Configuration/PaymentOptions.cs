namespace ProjectNamePlaceholder.Application.Common.Configuration;

// Provider-agnostic on purpose: the gateway choice (Stripe / Paddle / manual) is deliberately
// deferred, so nothing above this type may reference a provider SDK. Picking a provider later means
// adding an IPaymentGateway implementation and flipping Provider -- not reworking callers.
public sealed class PaymentOptions
{
    // Matches IPaymentGateway.Provider. "manual" is the default and means no gateway at all:
    // licences are extended by a platform admin, which is exactly how the system behaves today.
    public string Provider { get; set; } = "manual";

    // Master switch. When false the webhook endpoint refuses everything, so a half-configured
    // deployment cannot have licence state driven by unverified traffic.
    public bool Enabled { get; set; }

    // Verifies webhook authenticity. Without it any caller who guesses the URL could extend their
    // own licence indefinitely, so an enabled gateway MUST have one -- enforced at startup.
    public string? WebhookSecret { get; set; }

    public string? ApiKey { get; set; }
    public string? PublishableKey { get; set; }

    // ISO 4217. Plans store their own currency; this is the fallback for plans predating pricing.
    public string DefaultCurrency { get; set; } = "USD";

    // Where the provider's hosted checkout/portal returns the user.
    public string? CheckoutSuccessUrl { get; set; }
    public string? CheckoutCancelUrl { get; set; }

    // Applied on top of a paid period when extending a licence, absorbing clock skew and settlement
    // delay so a tenant is never locked out for the minutes between renewal and webhook delivery.
    // This is NOT the dunning window -- LicensingOptions.GracePeriodDays already covers that.
    public int RenewalSkewHours { get; set; } = 12;
}
