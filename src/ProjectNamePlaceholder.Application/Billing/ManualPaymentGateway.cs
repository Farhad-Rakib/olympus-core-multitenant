using ProjectNamePlaceholder.Application.Common.Exceptions;

namespace ProjectNamePlaceholder.Application.Billing;

// The default while no provider is chosen. It is a real, working mode -- not a stub: licences are
// extended by a platform admin through the existing SetLicenseExpiry/Suspend endpoints, which is
// exactly how the system behaves today. It refuses webhook and checkout calls rather than
// pretending to succeed, so a deployment that forgot to configure a provider fails loudly.
public sealed class ManualPaymentGateway : IPaymentGateway
{
    public string Provider => "manual";

    public Task<BillingEvent?> ParseWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default)
        => throw new AppException("No payment gateway is configured; billing webhooks are not accepted.", 501);

    public Task<string> CreateCheckoutUrlAsync(long tenantId, string planKey, CancellationToken cancellationToken = default)
        => throw new AppException("No payment gateway is configured; checkout is unavailable.", 501);

    public Task<string> CreateBillingPortalUrlAsync(long tenantId, CancellationToken cancellationToken = default)
        => throw new AppException("No payment gateway is configured; the billing portal is unavailable.", 501);
}
