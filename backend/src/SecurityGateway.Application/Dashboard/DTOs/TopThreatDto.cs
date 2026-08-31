namespace SecurityGateway.Application.Dashboard.DTOs;

public class TopThreatDto
{
    public required string IpAddress { get; set; }
    public int ThreatScore { get; set; }
    public long RequestCount { get; set; }
    public long AttackCount { get; set; }
}
