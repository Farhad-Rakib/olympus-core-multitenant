namespace ProjectNamePlaceholder.Application.Billing;

// Self-service side of billing: the tenant's own admin buying or managing their subscription.
// Everything here is scoped to the ambient tenant; the platform-admin licence controls live on
// ITenantService and take an explicit tenant id.
public interface ITenantBillingService
{
    TenantBillingConfigDto GetConfig();

    Task<IReadOnlyList<PurchasablePlanDto>> GetPurchasablePlansAsync(long tenantId, CancellationToken cancellationToken = default);

    Task<HostedUrlDto> StartCheckoutAsync(long tenantId, string planKey, CancellationToken cancellationToken = default);

    Task<HostedUrlDto> OpenBillingPortalAsync(long tenantId, CancellationToken cancellationToken = default);
}
