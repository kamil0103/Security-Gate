namespace SecurityGateway.Application.Notifications.DTOs;

public class SendTestNotificationRequest
{
    public required string Subject { get; set; }
    public required string Body { get; set; }
}
