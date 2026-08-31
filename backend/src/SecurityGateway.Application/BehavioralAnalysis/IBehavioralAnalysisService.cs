namespace SecurityGateway.Application.BehavioralAnalysis;

public interface IBehavioralAnalysisService
{
    Task<BehavioralAnalysisResult> AnalyzeAsync(BehavioralRequest request, CancellationToken cancellationToken = default);
}

public sealed class BehavioralRequest
{
    public required string IpAddress { get; set; }
    public Guid? UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public required string Path { get; set; }
    public required string Method { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class BehavioralAnalysisResult
{
    public int RiskScore { get; set; }
    public bool IsAnomalous { get; set; }
    public List<string> Reasons { get; set; } = new();
}
