namespace SecurityGateway.Application.ThreatDetection.Models;

public sealed record ThreatScoreResult
{
    public required string SourceIp { get; init; }
    public required int NewScore { get; init; }
    public required string ThreatLevel { get; init; }
    public required bool Escalated { get; init; }
    public string? Reason { get; init; }
}
