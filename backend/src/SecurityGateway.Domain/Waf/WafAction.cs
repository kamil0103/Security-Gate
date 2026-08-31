using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.Waf;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WafAction
{
    Allowed,
    Blocked,
    Logged
}
