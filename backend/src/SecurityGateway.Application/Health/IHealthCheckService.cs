namespace SecurityGateway.Application.Health;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
