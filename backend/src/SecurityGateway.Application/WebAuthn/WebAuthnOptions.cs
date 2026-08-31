namespace SecurityGateway.Application.WebAuthn;

public sealed class WebAuthnOptions
{
    public const string SectionName = "WebAuthn";

    public bool Enabled { get; set; }
    public string RelyingPartyId { get; set; } = "localhost";
    public string RelyingPartyName { get; set; } = "Security Gateway";
    public string Origin { get; set; } = "http://localhost:3100";
}
