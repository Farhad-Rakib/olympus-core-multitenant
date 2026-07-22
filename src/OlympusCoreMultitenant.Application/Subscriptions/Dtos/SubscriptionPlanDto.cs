namespace OlympusCoreMultitenant.Application.Subscriptions.Dtos;

public sealed record SubscriptionPlanDto(
    long Id,
    string Key,
    string Name,
    string Description,
    int MaxUsers,
    bool IsActive,
    IReadOnlyList<string> ModuleKeys);
