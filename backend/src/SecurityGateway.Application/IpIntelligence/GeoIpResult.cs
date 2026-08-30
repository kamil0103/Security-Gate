namespace SecurityGateway.Application.IpIntelligence;

public sealed record GeoIpResult
{
    public string? CountryCode { get; init; }
    public string? Country { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Isp { get; init; }
    public string? Organization { get; init; }
    public string? Asn { get; init; }
}
