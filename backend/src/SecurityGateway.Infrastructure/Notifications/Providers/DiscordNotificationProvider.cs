using System.Net.Http.Json;
using System.Text.Json;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Infrastructure.Notifications.Providers;

public sealed class DiscordNotificationProvider : INotificationChannelProvider
{
    private readonly HttpClient _httpClient;

    public DiscordNotificationProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanHandle(NotificationChannelType type) => type == NotificationChannelType.Discord;

    public async Task SendAsync(NotificationChannel channel, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<DiscordChannelConfiguration>(channel.Configuration)
            ?? new DiscordChannelConfiguration();

        if (string.IsNullOrWhiteSpace(config.WebhookUrl))
        {
            throw new InvalidOperationException("Discord channel is missing webhook URL.");
        }

        var color = message.Severity switch
        {
            SecurityEventSeverity.Critical => 16711680,
            SecurityEventSeverity.High => 16753920,
            SecurityEventSeverity.Medium => 16776960,
            SecurityEventSeverity.Low => 65280,
            _ => 3447003
        };

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = message.Title,
                    description = message.Body,
                    color,
                    fields = new[]
                    {
                        new { name = "Severity", value = message.Severity.ToString(), inline = true },
                        new { name = "Event", value = message.EventType ?? "N/A", inline = true },
                        new { name = "Source IP", value = message.SourceIp ?? "N/A", inline = true }
                    },
                    timestamp = message.Timestamp.UtcDateTime.ToString("O")
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(config.WebhookUrl, payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private sealed class DiscordChannelConfiguration
    {
        public string? WebhookUrl { get; set; }
    }
}
