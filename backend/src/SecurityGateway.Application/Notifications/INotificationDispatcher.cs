using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Application.Notifications;

public interface INotificationDispatcher
{
    Task DispatchAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
