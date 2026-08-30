namespace SecurityGateway.Application.IpIntelligence;

public sealed record VpnProxyResult
{
    public bool IsVpn { get; init; }
    public bool IsProxy { get; init; }
    public bool IsTor { get; init; }
    public bool IsDatacenter { get; init; }
    public string? Source { get; init; }
}
