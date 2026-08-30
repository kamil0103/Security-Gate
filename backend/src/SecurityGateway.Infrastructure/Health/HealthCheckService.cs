using System.Data;
using Npgsql;
using SecurityGateway.Application.Health;
using StackExchange.Redis;

namespace SecurityGateway.Infrastructure.Health;

public sealed class HealthCheckService : IHealthCheckService
{
    private readonly string _postgresConnectionString;
    private readonly string _redisConnectionString;

    public HealthCheckService(string postgresConnectionString, string redisConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        _postgresConnectionString = postgresConnectionString;
        _redisConnectionString = redisConnectionString;
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var postgresConnected = await CheckPostgresAsync(cancellationToken).ConfigureAwait(false);
        var redisConnected = await CheckRedisAsync(cancellationToken).ConfigureAwait(false);

        var status = postgresConnected && redisConnected ? "Healthy" : "Degraded";

        return new HealthCheckResult
        {
            Status = status,
            PostgresConnected = postgresConnected,
            RedisConnected = redisConnected,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private async Task<bool> CheckPostgresAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_postgresConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckRedisAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString).ConfigureAwait(false);
            var database = connection.GetDatabase();
            await database.PingAsync(CommandFlags.None).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
