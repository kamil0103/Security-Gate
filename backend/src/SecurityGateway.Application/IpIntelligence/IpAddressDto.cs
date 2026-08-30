namespace SecurityGateway.Application.IpIntelligence;

public sealed record IpAddressDto
{
    public required Guid Id { get; init; }
    public required string Ip { get; init; }
    public string? CountryCode { get; init; }
    public string? Country { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Isp { get; init; }
    public string? Asn { get; init; }
    public bool IsVpn { get; init; }
    public bool IsProxy { get; init; }
    public bool IsTor { get; init; }
    public bool IsDatacenter { get; init; }
    public int ThreatScore { get; init; }
    public string? ThreatLevel { get; init; }
    public long RequestCount { get; init; }
    public long AttackCount { get; init; }
    public long BlockCount { get; init; }
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}
