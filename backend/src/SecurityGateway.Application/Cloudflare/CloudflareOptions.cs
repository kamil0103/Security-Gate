namespace SecurityGateway.Application.Cloudflare;

public sealed class CloudflareOptions
{
    public const string SectionName = "Cloudflare";

    public bool Enabled { get; set; }
    public bool TrustConnectingIp { get; set; } = true;
    public bool TrustVisitorIp { get; set; }
    public List<string> IpRanges { get; set; } = new();
    public int RefreshIntervalHours { get; set; } = 24;
}
