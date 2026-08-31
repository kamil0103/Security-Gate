using System.Text.Json;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Infrastructure.Notifications.Providers;

public sealed class EmailNotificationProvider : INotificationChannelProvider
{
    private readonly IEmailService _emailService;

    public EmailNotificationProvider(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public bool CanHandle(NotificationChannelType type) => type == NotificationChannelType.Email;

    public async Task SendAsync(NotificationChannel channel, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<EmailChannelConfiguration>(channel.Configuration)
            ?? new EmailChannelConfiguration();

        if (string.IsNullOrWhiteSpace(config.To))
        {
            throw new InvalidOperationException("Email channel is missing recipient address.");
        }

        var subject = $"[Security Gateway] {message.Title}";
        var body = FormatBody(message);

        await _emailService.SendEmailAsync(config.To, subject, body, cancellationToken).ConfigureAwait(false);
    }

    private static string FormatBody(NotificationMessage message)
    {
        return $"Severity: {message.Severity}\n" +
               $"Event: {message.EventType ?? "N/A"}\n" +
               $"Source IP: {message.SourceIp ?? "N/A"}\n" +
               $"Timestamp: {message.Timestamp:O}\n\n" +
               $"{message.Body}";
    }

    private sealed class EmailChannelConfiguration
    {
        public string? To { get; set; }
    }
}
