namespace SecurityGateway.Application.ThreatIntelligence;

public sealed class ThreatIntelligenceOptions
{
    public const string SectionName = "ThreatIntelligence";

    public bool Enabled { get; set; }
    public string AbuseIpDbApiKey { get; set; } = string.Empty;
}
