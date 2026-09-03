using System.Net;
using SecurityGateway.Api;
using Xunit;

namespace SecurityGateway.Tests.Middleware;

public class SecurityHeadersIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SecurityHeadersIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_AdminHost_ReturnsStrictSecurityHeaders()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Host = "admin.toncom159.com";

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
        Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").FirstOrDefault());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").FirstOrDefault());
    }

    [Fact]
    public async Task HealthEndpoint_ProxiedHost_DoesNotSetBlanketCsp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Host = "app.example.com";

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
    }
}
