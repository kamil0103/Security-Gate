using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Application.Notifications.DTOs;

public class NotificationChannelDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public NotificationChannelType Type { get; set; }
    public bool IsEnabled { get; set; }
    public string Configuration { get; set; } = "{}";
}
