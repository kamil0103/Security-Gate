namespace SecurityGateway.Application.CrowdSec;

public sealed class CrowdSecOptions
{
    public const string SectionName = "CrowdSec";

    public bool Enabled { get; set; }
    public string ApiUrl { get; set; } = "http://crowdsec:8080";
    public string ApiKey { get; set; } = string.Empty;
}
