using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Subscriptions.Dtos;

public sealed record CreateSubscriptionPlanRequestDto(
    string Key,
    string Name,
    string Description,
    int MaxUsers,
    // Optional so existing callers keep working: a plan created without pricing is simply
    // not sold through a gateway yet.
    decimal Price = 0m,
    string Currency = "USD",
    BillingInterval BillingInterval = BillingInterval.None,
    string? ExternalPriceId = null,
    // 0 = platform default (RateLimiting:PermitLimit).
    int RequestsPerMinute = 0,
    IReadOnlyList<string>? ModuleKeys = null);
