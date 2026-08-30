using SecurityGateway.Application.RateLimiting;
using StackExchange.Redis;

namespace SecurityGateway.Infrastructure.RateLimiting.Stores;

public sealed class RedisRateLimitStore : IRateLimitStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisRateLimitStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public bool IsAvailable => _connectionMultiplexer.IsConnected;

    public async Task<RateLimitCounter> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var now = DateTimeOffset.UtcNow;
        var windowStart = new DateTimeOffset(now.Ticks - (now.Ticks % window.Ticks), TimeSpan.Zero);
        var windowEnd = windowStart.Add(window);
        var counterKey = $"{key}:{windowStart.ToUnixTimeSeconds()}";

        var count = await database.StringIncrementAsync(counterKey).ConfigureAwait(false);
        await database.KeyExpireAsync(counterKey, window).ConfigureAwait(false);

        return new RateLimitCounter
        {
            Count = count,
            WindowStart = windowStart,
            WindowEnd = windowEnd
        };
    }

    public async Task<RateLimitCounter> GetAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var now = DateTimeOffset.UtcNow;
        var windowStart = new DateTimeOffset(now.Ticks - (now.Ticks % window.Ticks), TimeSpan.Zero);
        var windowEnd = windowStart.Add(window);
        var counterKey = $"{key}:{windowStart.ToUnixTimeSeconds()}";

        var countValue = await database.StringGetAsync(counterKey).ConfigureAwait(false);
        var count = countValue.IsNull ? 0 : (long)countValue;

        return new RateLimitCounter
        {
            Count = count,
            WindowStart = windowStart,
            WindowEnd = windowEnd
        };
    }

    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{key}:*").ToArray();

        if (keys.Length > 0)
        {
            await database.KeyDeleteAsync(keys).ConfigureAwait(false);
        }
    }
}
