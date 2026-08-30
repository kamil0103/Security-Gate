using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Application.ThreatDetection.DTOs;

public sealed record SecurityEventDto
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required SecurityEventType Type { get; init; }
    public required SecurityEventSeverity Severity { get; init; }
    public required string SourceIp { get; init; }
    public Guid? UserId { get; init; }
    public Guid? DeviceId { get; init; }
    public string? Description { get; init; }
    public string? RelatedEntityType { get; init; }
    public string? RelatedEntityId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
