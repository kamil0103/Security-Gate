using SecurityGateway.Domain.Audit;

namespace SecurityGateway.Application.Audit.DTOs;

public class AuditLogFilterRequest
{
    public AuditCategory? Category { get; set; }
    public string? Action { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public bool? Success { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
}
