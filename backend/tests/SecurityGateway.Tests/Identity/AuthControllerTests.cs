using System.Net;
using System.Net.Http.Json;
using SecurityGateway.Application.Identity.DTOs;
using Xunit;

namespace SecurityGateway.Tests.Identity;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsUserAndTokens()
    {
        var client = _factory.CreateClient();
        var request = new RegisterWithDeviceRequest
        {
            User = new RegisterRequest
            {
                Username = "apitest",
                Email = "apitest@example.com",
                Password = "StrongPassword123!"
            }
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal("apitest", result.User.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.AccessToken));
        Assert.True(result.Device.IsTrusted);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var client = _factory.CreateClient();

        var registerRequest = new RegisterWithDeviceRequest
        {
            User = new RegisterRequest
            {
                Username = "logintest",
                Email = "logintest@example.com",
                Password = "StrongPassword123!"
            }
        };

        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginWithDeviceRequest
        {
            User = new LoginRequest
            {
                UsernameOrEmail = "logintest",
                Password = "StrongPassword123!"
            }
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.AccessToken));
    }

    [Fact]
    public async Task Me_WithoutAuthorization_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        var client = _factory.CreateClient();

        var registerRequest = new RegisterWithDeviceRequest
        {
            User = new RegisterRequest
            {
                Username = "metest",
                Email = "metest@example.com",
                Password = "StrongPassword123!"
            }
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Tokens.AccessToken);
        var response = await client.GetAsync("/api/auth/me");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(content), $"Response body was empty. Status: {response.StatusCode}");

        var result = System.Text.Json.JsonSerializer.Deserialize<UserDto>(content, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(result);
        Assert.Equal("metest", result.Username);
    }
}
