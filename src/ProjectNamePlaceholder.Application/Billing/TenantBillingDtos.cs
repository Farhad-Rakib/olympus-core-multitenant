using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Billing;

// What the frontend needs to decide whether to render "Subscribe" / "Manage billing" at all.
// CheckoutAvailable is false under the manual provider, where a platform admin extends licences
// by hand and there is no hosted checkout to send anyone to.
public sealed record TenantBillingConfigDto(string Provider, bool CheckoutAvailable, string DefaultCurrency);

// A plan as a tenant sees it when choosing what to buy: no ExternalPriceId (a provider detail),
// no module ids -- keys only, which is all the UI shows.
public sealed record PurchasablePlanDto(
    string Key,
    string Name,
    string Description,
    int MaxUsers,
    decimal Price,
    string Currency,
    BillingInterval BillingInterval,
    bool IsCurrent);

public sealed record StartCheckoutRequestDto(string PlanKey);

public sealed record HostedUrlDto(string Url);
