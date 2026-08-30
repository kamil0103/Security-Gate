using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SecurityGateway.Api;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Application.RateLimiting.DTOs;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.RateLimiting;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.RateLimiting;

public class RateLimitControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RateLimitControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateRule_Admin_ReturnsCreated()
    {
        var token = await CreateAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateRateLimitRuleRequest
        {
            ScopeType = RateLimitScopeType.Global,
            RequestsPerWindow = 100,
            WindowSeconds = 60
        };

        var response = await client.PostAsJsonAsync("/api/ratelimit", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RateLimitRuleDto>();
        Assert.NotNull(result);
        Assert.Equal(RateLimitScopeType.Global, result.ScopeType);
    }

    [Fact]
    public async Task GetRules_Admin_ReturnsOk()
    {
        var token = await CreateAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/ratelimit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RateLimit_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/ratelimit");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RateLimit_NonAdmin_ReturnsForbidden()
    {
        var token = await RegisterAndLoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/ratelimit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> CreateAdminTokenAsync()
    {
        var username = $"admin{Guid.NewGuid():N}";
        var password = "StrongPassword123!";

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var admin = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = passwordHasher.HashPassword(password),
            Role = UserRole.Administrator,
            Status = UserStatus.Active,
            EmailVerified = true
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var client = _factory.CreateClient();
        var loginRequest = new LoginWithDeviceRequest
        {
            User = new LoginRequest
            {
                UsernameOrEmail = username,
                Password = password
            }
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.EnsureSuccessStatusCode();
        return result!.Tokens.AccessToken;
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var username = $"user{Guid.NewGuid():N}";
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
