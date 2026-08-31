using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SecurityGateway.Api;
using SecurityGateway.Application.Dashboard.DTOs;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.IpIntelligence;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Domain.Waf;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.Dashboard;

public class DashboardControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DashboardControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOverview_Admin_ReturnsOk()
    {
        var token = await CreateAdminTokenAsync();
        SeedDashboardData();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/dashboard/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DashboardOverviewDto>();
        Assert.NotNull(result);
        Assert.True(result.TotalRequests >= 5);
    }

    [Fact]
    public async Task GetTopThreats_Admin_ReturnsThreats()
    {
        var token = await CreateAdminTokenAsync();
        SeedDashboardData();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/dashboard/top-threats?limit=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TopThreatDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetRecentEvents_Admin_ReturnsEvents()
    {
        var token = await CreateAdminTokenAsync();
        SeedDashboardData();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/dashboard/recent-events?limit=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<RecentEventDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Dashboard_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/dashboard/overview");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private void SeedDashboardData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.IpAddresses.Add(new IpAddress
        {
            Id = Guid.NewGuid(),
            Ip = "198.51.100.20",
            RequestCount = 5,
            ThreatScore = 75
        });

        context.SecurityEvents.Add(new SecurityEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Type = SecurityEventType.AccessBlocked,
            Severity = SecurityEventSeverity.High,
            SourceIp = "198.51.100.20"
        });

        context.WafEvents.Add(new WafEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            SourceIp = "198.51.100.20",
            RuleId = "123",
            Method = "GET",
            Uri = "/",
            AttackType = AttackType.SqlInjection
        });

        context.SaveChanges();
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
}
