using SecurityGateway.Domain.Audit;

namespace SecurityGateway.Application.Audit.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public AuditCategory Category { get; set; }
    public required string Action { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
}
