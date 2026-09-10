using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Subscriptions.Dtos;

public sealed record SubscriptionPlanDto(
    long Id,
    string Key,
    string Name,
    string Description,
    int MaxUsers,
    bool IsActive,
    decimal Price,
    string Currency,
    BillingInterval BillingInterval,
    string? ExternalPriceId,
    IReadOnlyList<string> ModuleKeys);
