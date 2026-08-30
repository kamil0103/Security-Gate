using SecurityGateway.Application.Health;
using SecurityGateway.Infrastructure.Health;
using Xunit;

namespace SecurityGateway.Tests.Health;

public class HealthCheckServiceTests
{
    [Fact]
    public async Task CheckAsync_WithValidConnectionStrings_ReturnsResult()
    {
        var postgres = "Host=localhost;Database=securitygateway;Username=securitygateway;Password=ChangeMeInProduction123!";
        var redis = "localhost:6379,password=ChangeMeInProduction456!";
        var service = new HealthCheckService(postgres, redis);

        var result = await service.CheckAsync();

        Assert.NotNull(result);
        Assert.True(result.Status == "Healthy" || result.Status == "Degraded");
        Assert.True(result.Timestamp <= DateTimeOffset.UtcNow);
    }
}
