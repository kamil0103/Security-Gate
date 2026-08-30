namespace SecurityGateway.Domain.RateLimiting;

public sealed class RateLimitRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required RateLimitScopeType ScopeType { get; init; }
    public string? ScopeValue { get; init; }
    public required int RequestsPerWindow { get; set; }
    public required int WindowSeconds { get; set; }
    public int BurstAllowance { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
