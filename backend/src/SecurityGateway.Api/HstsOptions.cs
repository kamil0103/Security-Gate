namespace SecurityGateway.Api;

public sealed class HstsOptions
{
    public const string SectionName = "Hsts";

    public bool Enabled { get; set; } = true;
    public int MaxAgeDays { get; set; } = 365;
    public bool IncludeSubDomains { get; set; } = true;
    public bool Preload { get; set; } = false;
}
