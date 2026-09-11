using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Billing;

public sealed class TenantBillingService : ITenantBillingService
{
    private const string ManualProvider = "manual";

    private readonly IPaymentGateway _paymentGateway;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly PaymentOptions _paymentOptions;

    public TenantBillingService(
        IPaymentGateway paymentGateway,
        IRepository<Tenant> tenantRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IOptions<PaymentOptions> paymentOptions)
    {
        _paymentGateway = paymentGateway;
        _tenantRepository = tenantRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _paymentOptions = paymentOptions.Value;
    }

    public TenantBillingConfigDto GetConfig()
    {
        var provider = _paymentGateway.Provider.Trim().ToLowerInvariant();
        return new TenantBillingConfigDto(
            provider,
            CheckoutAvailable: _paymentOptions.Enabled && provider != ManualProvider,
            _paymentOptions.DefaultCurrency);
    }

    public async Task<IReadOnlyList<PurchasablePlanDto>> GetPurchasablePlansAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantAsync(tenantId, cancellationToken);
        var plans = await _subscriptionPlanRepository.GetAllAsync(cancellationToken);

        // The tenant's current plan is always listed (even if since deactivated or unpriced) so the
        // page can say "you are on X"; everything else must be active AND actually sellable.
        return plans
            .Where(plan => plan.Id == tenant.SubscriptionPlanId || (plan.IsActive && IsSellable(plan)))
            .OrderBy(plan => plan.Price)
            .ThenBy(plan => plan.Name)
            .Select(plan => new PurchasablePlanDto(
                plan.Key,
                plan.Name,
                plan.Description,
                plan.MaxUsers,
                plan.Price,
                plan.Currency,
                plan.BillingInterval,
                IsCurrent: plan.Id == tenant.SubscriptionPlanId))
            .ToList();
    }

    public async Task<HostedUrlDto> StartCheckoutAsync(long tenantId, string planKey, CancellationToken cancellationToken = default)
    {
        EnsureCheckoutAvailable();
        await GetTenantAsync(tenantId, cancellationToken);

        var normalizedKey = planKey?.Trim().ToLowerInvariant() ?? string.Empty;
        var plan = await _subscriptionPlanRepository.GetByKeyAsync(normalizedKey, cancellationToken)
            ?? throw new AppException("Subscription plan not found.", 404);

        if (!plan.IsActive)
        {
            throw new AppException("This subscription plan is no longer available.", 409);
        }

        // A plan with no recurring price or no provider price id cannot be sold through a hosted
        // checkout -- it is managed by a platform admin. Say so rather than letting the gateway
        // fail with a provider-specific error.
        if (!IsSellable(plan))
        {
            throw new AppException("This subscription plan is not available for self-service purchase.", 409);
        }

        var url = await _paymentGateway.CreateCheckoutUrlAsync(tenantId, plan.Key, cancellationToken);
        return new HostedUrlDto(url);
    }

    public async Task<HostedUrlDto> OpenBillingPortalAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        EnsureCheckoutAvailable();
        await GetTenantAsync(tenantId, cancellationToken);

        var url = await _paymentGateway.CreateBillingPortalUrlAsync(tenantId, cancellationToken);
        return new HostedUrlDto(url);
    }

    private static bool IsSellable(SubscriptionPlan plan) =>
        plan.BillingInterval != BillingInterval.None
        && plan.Price > 0
        && !string.IsNullOrWhiteSpace(plan.ExternalPriceId);

    private void EnsureCheckoutAvailable()
    {
        // 501 matches what ManualPaymentGateway itself returns: "this deployment has no such
        // capability", as opposed to 4xx "you did something wrong".
        if (!GetConfig().CheckoutAvailable)
        {
            throw new AppException("Self-service billing is not enabled on this platform. Contact your administrator to change your plan.", 501);
        }
    }

    private async Task<Tenant> GetTenantAsync(long tenantId, CancellationToken cancellationToken) =>
        await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);
}
