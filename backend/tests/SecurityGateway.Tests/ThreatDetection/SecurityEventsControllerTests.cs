using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SecurityGateway.Api;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Application.ThreatDetection.DTOs;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.ThreatDetection;

public class SecurityEventsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SecurityEventsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSecurityEvent_Admin_ReturnsCreated()
    {
        var token = await CreateAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateSecurityEventRequest
        {
            Type = SecurityEventType.AuthenticationFailure,
            Severity = SecurityEventSeverity.Low,
            SourceIp = "198.51.100.1",
            Description = "Test event"
        };

        var response = await client.PostAsJsonAsync("/api/securityevents", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SecurityEventDto>();
        Assert.NotNull(result);
        Assert.Equal(SecurityEventType.AuthenticationFailure, result.Type);
    }

    [Fact]
    public async Task GetRecent_Admin_ReturnsOk()
    {
        var token = await CreateAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/securityevents/recent?count=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecurityEvents_NonAdmin_ReturnsForbidden()
    {
        var token = await RegisterAndLoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/securityevents/recent");

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
