using SecurityGateway.Domain.RateLimiting;

namespace SecurityGateway.Application.RateLimiting.DTOs;

public sealed record CreateRateLimitRuleRequest
{
    public required RateLimitScopeType ScopeType { get; init; }
    public string? ScopeValue { get; init; }
    public required int RequestsPerWindow { get; init; }
    public required int WindowSeconds { get; init; }
    public int BurstAllowance { get; init; }
}
