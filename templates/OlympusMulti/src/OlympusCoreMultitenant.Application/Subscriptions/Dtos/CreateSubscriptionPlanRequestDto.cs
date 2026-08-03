namespace OlympusCoreMultitenant.Application.Subscriptions.Dtos;

public sealed record CreateSubscriptionPlanRequestDto(
    string Key,
    string Name,
    string Description,
    int MaxUsers,
    IReadOnlyList<string>? ModuleKeys = null);
