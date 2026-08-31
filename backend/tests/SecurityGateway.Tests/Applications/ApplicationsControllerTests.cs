using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SecurityGateway.Api;
using SecurityGateway.Application.Applications.DTOs;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.Applications;

public class ApplicationsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ApplicationsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateApplication_Admin_ReturnsCreated()
    {
        var token = await CreateAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateApplicationRequest
        {
            Name = "Immich",
            Domain = "photos.example.com",
            UpstreamUrl = "http://localhost:3001"
        };

        var response = await client.PostAsJsonAsync("/api/applications", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.NotNull(result.Policy);
    }

    [Fact]
    public async Task GetApplications_Admin_ReturnsOk()
    {
        var token = await CreateAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePolicy_Admin_ReturnsOk()
    {
        var token = await CreateAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateApplicationRequest
        {
            Name = "Test",
            Domain = $"test{Guid.NewGuid():N}.example.com",
            UpstreamUrl = "http://localhost:3001"
        };

        var createResponse = await client.PostAsJsonAsync("/api/applications", createRequest);
        var app = await createResponse.Content.ReadFromJsonAsync<ApplicationDto>();
        createResponse.EnsureSuccessStatusCode();

        var policyRequest = new UpdateApplicationPolicyRequest
        {
            RequireAuthentication = false,
            AllowAnonymousFromTrustedNetworks = true
        };

        var response = await client.PutAsJsonAsync($"/api/applications/{app!.Id}/policy", policyRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var policy = await response.Content.ReadFromJsonAsync<ApplicationPolicyDto>();
        Assert.NotNull(policy);
        Assert.False(policy.RequireAuthentication);
    }

    [Fact]
    public async Task Applications_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Applications_NonAdmin_ReturnsForbidden()
    {
        var token = await RegisterAndLoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/applications");

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
