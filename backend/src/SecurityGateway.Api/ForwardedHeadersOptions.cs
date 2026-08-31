namespace SecurityGateway.Api;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; set; } = true;
    public List<string> KnownProxies { get; set; } = new();
    public List<string> KnownNetworks { get; set; } = new();
    public bool ForwardHeaders { get; set; } = true;
}
