namespace SecurityGateway.Domain.Notifications;

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

public sealed class NotificationLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ChannelId { get; set; }
    public NotificationChannelType ChannelType { get; set; }
    public required string Recipient { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
}
