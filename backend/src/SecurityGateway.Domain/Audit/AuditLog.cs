namespace SecurityGateway.Domain.Audit;

public enum AuditCategory
{
    Authentication,
    Authorization,
    AccessControl,
    Blocking,
    Application,
    RateLimiting,
    Waf,
    ThreatDetection,
    Notification,
    System
}

public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public AuditCategory Category { get; init; }
    public required string Action { get; init; }
    public Guid? UserId { get; init; }
    public string? Username { get; init; }
    public string? IpAddress { get; init; }
    public string? Details { get; init; }
    public bool Success { get; init; } = true;
}
