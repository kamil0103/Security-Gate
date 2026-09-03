using SecurityGateway.Application.RateLimiting;

namespace SecurityGateway.Tests.TestHelpers;

public sealed class UnavailableRateLimitStore : IRateLimitStore
{
    public bool IsAvailable => false;

    public Task<RateLimitCounter> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Store is unavailable.");
    }

    public Task<RateLimitCounter> GetAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Store is unavailable.");
    }

    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Store is unavailable.");
    }
}
