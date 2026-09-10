using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Billing;

// Translates a normalised BillingEvent into the licence actions that already existed
// (SetLicenseExpiry / Suspend / Unsuspend / AssignSubscriptionPlan). Nothing here knows which
// provider produced the event.
public sealed class BillingEventProcessor : IBillingEventProcessor
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IProcessedBillingEventRepository _processedEventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentOptions _paymentOptions;

    public BillingEventProcessor(
        IRepository<Tenant> tenantRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IProcessedBillingEventRepository processedEventRepository,
        IUnitOfWork unitOfWork,
        IOptions<PaymentOptions> paymentOptions)
    {
        _tenantRepository = tenantRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _processedEventRepository = processedEventRepository;
        _unitOfWork = unitOfWork;
        _paymentOptions = paymentOptions.Value;
    }

    public async Task<BillingEventResult> ProcessAsync(BillingEvent billingEvent, CancellationToken cancellationToken = default)
    {
        if (await _processedEventRepository.ExistsAsync(_paymentOptions.Provider, billingEvent.ExternalEventId, cancellationToken))
        {
            return BillingEventResult.DuplicateIgnored;
        }

        var tenant = await _tenantRepository.GetByIdAsync(billingEvent.TenantId, cancellationToken);
        if (tenant is null)
        {
            // Reported rather than thrown: a 500 makes the provider retry this forever, and no
            // number of retries will conjure up a tenant that does not exist.
            return BillingEventResult.UnknownTenant;
        }

        switch (billingEvent.Type)
        {
            case BillingEventType.SubscriptionRenewed:
                if (billingEvent.PeriodEndUtc is { } periodEnd)
                {
                    // Skew absorbs settlement delay so a tenant is never locked out for the minutes
                    // between a successful payment and the webhook arriving.
                    tenant.SetLicenseExpiry(periodEnd.AddHours(_paymentOptions.RenewalSkewHours));
                }

                // A renewal after dunning must lift the suspension, or the tenant stays locked out
                // having already paid.
                tenant.Unsuspend();
                break;

            case BillingEventType.PaymentFailed:
                // Deliberately no state change. LicenseExpiresAtUtc simply stops being extended, so
                // the tenant slides into GracePeriod and then Expired on its own while the provider
                // retries the card. Suspending here would lock out a customer whose second attempt
                // is about to succeed.
                break;

            case BillingEventType.SubscriptionCancelled:
                tenant.Suspend();
                break;

            case BillingEventType.PlanChanged:
                if (!string.IsNullOrWhiteSpace(billingEvent.PlanKey))
                {
                    var plan = await _subscriptionPlanRepository.GetByKeyAsync(billingEvent.PlanKey, cancellationToken);
                    if (plan is null)
                    {
                        return BillingEventResult.UnknownPlan;
                    }

                    tenant.AssignSubscriptionPlan(plan.Id);
                }
                break;
        }

        // Written in the SAME transaction as the licence change: if this insert loses the unique
        // index race against a concurrent duplicate delivery, the licence change rolls back with
        // it rather than being applied twice.
        await _processedEventRepository.AddAsync(new ProcessedBillingEvent
        {
            Provider = _paymentOptions.Provider,
            ExternalEventId = billingEvent.ExternalEventId,
            TenantId = billingEvent.TenantId,
            EventType = billingEvent.Type.ToString(),
            ProcessedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return billingEvent.Type == BillingEventType.PaymentFailed
            ? BillingEventResult.PaymentFailureNoted
            : BillingEventResult.Applied;
    }
}
