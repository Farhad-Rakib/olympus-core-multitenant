namespace OlympusCoreMultitenant.Application.Notifications.Dtos;

public sealed record CreateNotificationRequestDto(
    Guid UserId,
    string Type,
    string Message
);
