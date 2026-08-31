namespace SecurityGateway.Application.ThreatIntelligence;

public sealed class ThreatIntelligenceResult
{
    public string Source { get; set; } = "Unknown";
    public bool IsMalicious { get; set; }
    public int ConfidenceScore { get; set; }
    public List<string> Categories { get; set; } = new();
    public string? CountryCode { get; set; }
    public string? Isp { get; set; }
    public string? RawData { get; set; }
}
