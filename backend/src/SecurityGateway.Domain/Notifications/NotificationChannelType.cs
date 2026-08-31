using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.Notifications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationChannelType
{
    Email,
    Telegram,
    Discord,
    Ntfy,
    WebPush
}
