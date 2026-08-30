namespace SecurityGateway.Application.RateLimiting;

public interface IRateLimitStore
{
    Task<RateLimitCounter> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);
    Task<RateLimitCounter> GetAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
}

public sealed record RateLimitCounter
{
    public required long Count { get; init; }
    public required DateTimeOffset WindowStart { get; init; }
    public required DateTimeOffset WindowEnd { get; init; }
}
