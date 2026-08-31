using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Application.ThreatDetection.DTOs;

public sealed record ThreatScoreRuleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required SecurityEventType EventType { get; init; }
    public required int EventCountThreshold { get; init; }
    public required int TimeWindowSeconds { get; init; }
    public required int ScoreImpact { get; init; }
    public SecurityEventSeverity Severity { get; init; }
    public bool IsEnabled { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
