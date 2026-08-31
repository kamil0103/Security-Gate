namespace SecurityGateway.Application.Dashboard.DTOs;

public class RecentEventDto
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string Severity { get; set; }
    public required string SourceIp { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
}
