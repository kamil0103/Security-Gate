namespace SecurityGateway.Application.Health;

public sealed record HealthCheckResult
{
    public required string Status { get; init; }
    public required bool PostgresConnected { get; init; }
    public required bool RedisConnected { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
