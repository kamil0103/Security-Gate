namespace SecurityGateway.Domain.ThreatDetection;

public sealed class ThreatScoreRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required SecurityEventType EventType { get; set; }
    public required int EventCountThreshold { get; set; }
    public required int TimeWindowSeconds { get; set; }
    public required int ScoreImpact { get; set; }
    public SecurityEventSeverity Severity { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
