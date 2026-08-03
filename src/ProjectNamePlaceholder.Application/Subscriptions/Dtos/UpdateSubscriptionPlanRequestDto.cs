namespace ProjectNamePlaceholder.Application.Subscriptions.Dtos;

public sealed record UpdateSubscriptionPlanRequestDto(
    string Name,
    string Description,
    int MaxUsers,
    IReadOnlyList<string>? ModuleKeys = null);
