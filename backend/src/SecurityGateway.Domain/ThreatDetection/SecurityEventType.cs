using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.ThreatDetection;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SecurityEventType
{
    AuthenticationFailure,
    AccountLocked,
    RateLimitExceeded,
    WafEvent,
    AccessBlocked,
    UnknownDevice,
    NewDeviceFromUntrustedNetwork,
    IpReputationChanged,
    PolicyViolation,
    Custom
}
