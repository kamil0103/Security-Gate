namespace SecurityGateway.Application.Map.DTOs;

public class MapPointDto
{
    public required string IpAddress { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public int ThreatScore { get; set; }
    public long RequestCount { get; set; }
    public long AttackCount { get; set; }
    public DateTime LastSeenAt { get; set; }
}
