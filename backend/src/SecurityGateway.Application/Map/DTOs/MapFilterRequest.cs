namespace SecurityGateway.Application.Map.DTOs;

public class MapFilterRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? CountryCode { get; set; }
    public int? MinThreatScore { get; set; }
    public bool? HasAttacks { get; set; }
    public bool? IsBlocked { get; set; }
    public int Limit { get; set; } = 1000;
}
