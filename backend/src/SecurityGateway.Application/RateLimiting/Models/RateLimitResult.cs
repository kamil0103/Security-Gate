namespace SecurityGateway.Application.RateLimiting.Models;

public sealed record RateLimitResult
{
    public required bool Allowed { get; init; }
    public required int Remaining { get; init; }
    public required DateTimeOffset ResetAt { get; init; }
    public string? Reason { get; init; }
    public bool EscalatedToBlock { get; init; }
}
