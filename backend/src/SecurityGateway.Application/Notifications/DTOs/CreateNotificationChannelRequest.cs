using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Application.Notifications.DTOs;

public class CreateNotificationChannelRequest
{
    public required string Name { get; set; }
    public NotificationChannelType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Configuration { get; set; } = "{}";
}
