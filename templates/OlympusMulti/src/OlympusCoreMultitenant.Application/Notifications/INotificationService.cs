using OlympusCoreMultitenant.Application.Notifications.Dtos;

namespace OlympusCoreMultitenant.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(long userId, string type, string message, CancellationToken cancellationToken = default);
    Task DeleteNotificationAsync(long notificationId, CancellationToken cancellationToken = default);
}
