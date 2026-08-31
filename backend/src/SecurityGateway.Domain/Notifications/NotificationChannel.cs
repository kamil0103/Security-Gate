namespace SecurityGateway.Domain.Notifications;

public sealed class NotificationChannel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public NotificationChannelType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Configuration { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
