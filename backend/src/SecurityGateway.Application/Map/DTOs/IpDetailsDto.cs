namespace SecurityGateway.Application.Map.DTOs;

public class IpDetailsDto
{
    public required string IpAddress { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Isp { get; set; }
    public string? Organization { get; set; }
    public string? Asn { get; set; }
    public bool IsVpn { get; set; }
    public bool IsProxy { get; set; }
    public bool IsTor { get; set; }
    public bool IsDatacenter { get; set; }
    public int ThreatScore { get; set; }
    public string? ThreatLevel { get; set; }
    public long RequestCount { get; set; }
    public long AttackCount { get; set; }
    public long BlockCount { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
