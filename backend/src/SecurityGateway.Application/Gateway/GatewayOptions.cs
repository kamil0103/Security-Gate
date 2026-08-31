namespace SecurityGateway.Application.Gateway;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>
    /// The upstream Nginx Proxy Manager URL to which proxied requests are forwarded.
    /// </summary>
    public string UpstreamNpmUrl { get; set; } = "http://localhost:80";

    /// <summary>
    /// Comma-separated list of trusted proxy IP addresses or CIDR ranges.
    /// Only headers from these sources are trusted for client IP resolution.
    /// </summary>
    public string TrustedProxies { get; set; } = "127.0.0.1,::1";

    /// <summary>
    /// Path prefixes that are served directly by the gateway and not proxied upstream.
    /// Only applies when the request host matches <see cref="AdminDomain"/>.
    /// </summary>
    public List<string> AdminPathPrefixes { get; set; } = ["/api", "/swagger"];

    /// <summary>
    /// Host name of the admin dashboard. Admin path prefixes are only bypassed for this host.
    /// </summary>
    public string AdminDomain { get; set; } = "admin.toncom159.com";
}
