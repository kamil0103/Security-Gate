using System.Text.Json;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Infrastructure.Notifications.Providers;

public sealed class WebPushNotificationProvider : INotificationChannelProvider
{
    public bool CanHandle(NotificationChannelType type) => type == NotificationChannelType.WebPush;

    public Task SendAsync(NotificationChannel channel, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<WebPushChannelConfiguration>(channel.Configuration)
            ?? new WebPushChannelConfiguration();

        if (string.IsNullOrWhiteSpace(config.PublicKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
        {
            throw new InvalidOperationException("WebPush channel is missing VAPID keys.");
        }

        // Web Push requires subscriber subscriptions and a push service library.
        // This provider is a placeholder that validates configuration and logs.
        return Task.CompletedTask;
    }

    private sealed class WebPushChannelConfiguration
    {
        public string? PublicKey { get; set; }
        public string? PrivateKey { get; set; }
        public string? Subject { get; set; }
    }
}
