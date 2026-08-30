using System.Net;
using System.Net.Http.Json;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;
using Xunit;

namespace SecurityGateway.Tests.Identity;

public class DevicesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DevicesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyDevices_WithoutAuthorization_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyDevices_WithValidToken_ReturnsDevices()
    {
        var (client, tokens) = await RegisterAndLoginAsync("devicesuser", "devices@example.com");

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await client.GetAsync("/api/devices");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var devices = System.Text.Json.JsonSerializer.Deserialize<List<DeviceDto>>(content, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(devices);
        Assert.Single(devices);
    }

    [Fact]
    public async Task TrustDevice_WithValidToken_UpdatesTrustStatus()
    {
        var (client, tokens) = await RegisterAndLoginAsync("trustuser", "trust@example.com");

        // Register a second device as pending.
        var loginRequest = new LoginWithDeviceRequest
        {
            User = new LoginRequest { UsernameOrEmail = "trustuser", Password = "StrongPassword123!" },
            Device = new DeviceEnrollmentRequest
            {
                DeviceId = "second-device",
                Name = "Second Device",
                Fingerprint = "second-fingerprint"
            }
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        Assert.Equal(DeviceTrustStatus.Pending, loginResult.Device.TrustStatus);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var deviceId = loginResult.Device.Device!.Id;

        var response = await client.PostAsync($"/api/devices/{deviceId}/trust", null);

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<(HttpClient Client, TokenPair Tokens)> RegisterAndLoginAsync(string username, string email)
    {
        var client = _factory.CreateClient();

        var registerRequest = new RegisterWithDeviceRequest
        {
            User = new RegisterRequest
            {
                Username = username,
                Email = email,
                Password = "StrongPassword123!"
            }
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var result = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        return (client, result.Tokens);
    }
}
