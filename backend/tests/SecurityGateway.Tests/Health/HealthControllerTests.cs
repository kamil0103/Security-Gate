using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SecurityGateway.Application.Health;
using Xunit;

namespace SecurityGateway.Tests.Health;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ReturnsHealthCheckResult()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected OK or ServiceUnavailable, got {response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<HealthCheckResult>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Status));
        Assert.True(result.Timestamp <= DateTimeOffset.UtcNow);
    }
}
