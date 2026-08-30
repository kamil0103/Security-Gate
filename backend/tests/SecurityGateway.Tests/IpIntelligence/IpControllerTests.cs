using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SecurityGateway.Api;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Application.IpIntelligence;
using Xunit;

namespace SecurityGateway.Tests.IpIntelligence;

public class IpControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public IpControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyIp_Anonymous_ReturnsIpDto()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/ip/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRecent_Authorized_ReturnsOk()
    {
        var token = await RegisterAndLoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/ip/recent?count=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var token = await RegisterAndLoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/ip/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var service = _factory.Services.GetRequiredService<IIpIntelligenceService>();
        var tracked = await service.TrackAsync(new TrackIpRequest { IpAddress = "198.51.100.99" });

        var token = await RegisterAndLoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/ip/{tracked.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRecent_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/ip/recent");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var username = $"iptest{Guid.NewGuid():N}";
        var client = _factory.CreateClient();

        var registerRequest = new RegisterWithDeviceRequest
        {
            User = new RegisterRequest
            {
                Username = username,
                Email = $"{username}@example.com",
                Password = "StrongPassword123!"
            }
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new LoginWithDeviceRequest
        {
            User = new LoginRequest
            {
                UsernameOrEmail = username,
                Password = "StrongPassword123!"
            }
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.EnsureSuccessStatusCode();
        return result!.Tokens.AccessToken;
    }
}
