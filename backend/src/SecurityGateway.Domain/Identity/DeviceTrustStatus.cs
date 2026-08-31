using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.Identity;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceTrustStatus
{
    Pending = 0,
    Trusted = 1,
    Untrusted = 2,
    Blocked = 3
}
