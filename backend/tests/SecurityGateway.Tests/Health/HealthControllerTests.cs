using System.Net;
using System.Net.Http.Json;
using SecurityGateway.Application.Health;
using Xunit;

namespace SecurityGateway.Tests.Health;

public class HealthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HealthControllerTests(TestWebApplicationFactory factory)
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
