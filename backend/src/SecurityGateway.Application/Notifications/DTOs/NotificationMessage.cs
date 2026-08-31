using SecurityGateway.Domain.Notifications;
using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Application.Notifications.DTOs;

public class NotificationMessage
{
    public required string Title { get; set; }
    public required string Body { get; set; }
    public SecurityEventSeverity Severity { get; set; }
    public string? SourceIp { get; set; }
    public string? EventType { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
