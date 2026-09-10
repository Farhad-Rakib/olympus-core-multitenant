using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Subscriptions.Dtos;

public sealed record UpdateSubscriptionPlanRequestDto(
    string Name,
    string Description,
    int MaxUsers,
    // Optional so existing callers keep working: a plan created without pricing is simply
    // not sold through a gateway yet.
    decimal Price = 0m,
    string Currency = "USD",
    BillingInterval BillingInterval = BillingInterval.None,
    string? ExternalPriceId = null,
    IReadOnlyList<string>? ModuleKeys = null);
