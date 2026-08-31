namespace SecurityGateway.Application.Dashboard.DTOs;

public class SecurityEventSeriesDto
{
    public required string Severity { get; set; }
    public required List<TimeSeriesPointDto> Points { get; set; }
}
