namespace SecurityGateway.Domain.ThreatDetection;

public sealed class SecurityEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required DateTimeOffset Timestamp { get; init; }
    public required SecurityEventType Type { get; init; }
    public required SecurityEventSeverity Severity { get; init; }
    public required string SourceIp { get; init; }
    public Guid? UserId { get; init; }
    public Guid? DeviceId { get; init; }
    public string? Description { get; set; }
    public string? RelatedEntityType { get; init; }
    public string? RelatedEntityId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
