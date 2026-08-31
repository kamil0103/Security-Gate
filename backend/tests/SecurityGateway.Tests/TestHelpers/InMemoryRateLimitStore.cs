using SecurityGateway.Application.RateLimiting;

namespace SecurityGateway.Tests.TestHelpers;

public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly Dictionary<string, RateLimitCounter> _store = new();

    public bool IsAvailable => true;

    public Task<RateLimitCounter> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = new DateTimeOffset(now.Ticks - (now.Ticks % window.Ticks), TimeSpan.Zero);
        var counterKey = $"{key}:{windowStart.ToUnixTimeSeconds()}";

        lock (_store)
        {
            if (!_store.TryGetValue(counterKey, out var counter) || counter.WindowEnd < now)
            {
                counter = new RateLimitCounter
                {
                    Count = 0,
                    WindowStart = windowStart,
                    WindowEnd = windowStart.Add(window)
                };
            }

            counter = counter with { Count = counter.Count + 1 };
            _store[counterKey] = counter;

            return Task.FromResult(counter);
        }
    }

    public Task<RateLimitCounter> GetAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = new DateTimeOffset(now.Ticks - (now.Ticks % window.Ticks), TimeSpan.Zero);
        var counterKey = $"{key}:{windowStart.ToUnixTimeSeconds()}";

        lock (_store)
        {
            if (_store.TryGetValue(counterKey, out var counter) && counter.WindowEnd >= now)
            {
                return Task.FromResult(counter);
            }

            return Task.FromResult(new RateLimitCounter
            {
                Count = 0,
                WindowStart = windowStart,
                WindowEnd = windowStart.Add(window)
            });
        }
    }

    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_store)
        {
            var keysToRemove = _store.Keys.Where(k => k.StartsWith(key + ":", StringComparison.Ordinal)).ToList();

            foreach (var k in keysToRemove)
            {
                _store.Remove(k);
            }
        }

        return Task.CompletedTask;
    }
}
