using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Application.ThreatDetection.DTOs;

public sealed record SecurityEventFilter
{
    public SecurityEventType? Type { get; init; }
    public SecurityEventSeverity? Severity { get; init; }
    public string? SourceIp { get; init; }
    public Guid? UserId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
