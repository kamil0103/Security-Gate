using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Application.Notifications;

public interface INotificationChannelProvider
{
    bool CanHandle(NotificationChannelType type);
    Task SendAsync(NotificationChannel channel, NotificationMessage message, CancellationToken cancellationToken = default);
}
