using System.Net.Http.Json;
using System.Text.Json;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Infrastructure.Notifications.Providers;

public sealed class TelegramNotificationProvider : INotificationChannelProvider
{
    private readonly HttpClient _httpClient;

    public TelegramNotificationProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanHandle(NotificationChannelType type) => type == NotificationChannelType.Telegram;

    public async Task SendAsync(NotificationChannel channel, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<TelegramChannelConfiguration>(channel.Configuration)
            ?? new TelegramChannelConfiguration();

        if (string.IsNullOrWhiteSpace(config.BotToken) || string.IsNullOrWhiteSpace(config.ChatId))
        {
            throw new InvalidOperationException("Telegram channel is missing bot token or chat ID.");
        }

        var text = $"*{EscapeMarkdown(message.Title)}*\n" +
                   $"Severity: {message.Severity}\n" +
                   $"Event: {message.EventType ?? "N/A"}\n" +
                   $"Source IP: {EscapeMarkdown(message.SourceIp ?? "N/A")}\n" +
                   $"{EscapeMarkdown(message.Body)}";

        var url = $"https://api.telegram.org/bot{config.BotToken}/sendMessage";
        var payload = new
        {
            chat_id = config.ChatId,
            text,
            parse_mode = "MarkdownV2"
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static string EscapeMarkdown(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }

    private sealed class TelegramChannelConfiguration
    {
        public string? BotToken { get; set; }
        public string? ChatId { get; set; }
    }
}
