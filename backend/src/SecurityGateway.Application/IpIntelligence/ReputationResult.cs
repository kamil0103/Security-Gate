namespace SecurityGateway.Application.IpIntelligence;

public sealed record ReputationResult
{
    public int Score { get; init; }
    public string? ThreatLevel { get; init; }
    public string? Source { get; init; }
}
