using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Infrastructure.Notifications.Providers;

public sealed class NtfyNotificationProvider : INotificationChannelProvider
{
    private readonly HttpClient _httpClient;

    public NtfyNotificationProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanHandle(NotificationChannelType type) => type == NotificationChannelType.Ntfy;

    public async Task SendAsync(NotificationChannel channel, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<NtfyChannelConfiguration>(channel.Configuration)
            ?? new NtfyChannelConfiguration();

        if (string.IsNullOrWhiteSpace(config.Topic))
        {
            throw new InvalidOperationException("ntfy channel is missing topic.");
        }

        var serverUrl = config.ServerUrl?.TrimEnd('/') ?? "https://ntfy.sh";
        var url = $"{serverUrl}/{config.Topic}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(message.Body, Encoding.UTF8)
        };

        request.Headers.Add("Title", message.Title);
        request.Headers.Add("Tags", "warning");

        if (!string.IsNullOrWhiteSpace(config.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private sealed class NtfyChannelConfiguration
    {
        public string? ServerUrl { get; set; }
        public string? Topic { get; set; }
        public string? AccessToken { get; set; }
    }
}
