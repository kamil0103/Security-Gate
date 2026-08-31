using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.ThreatDetection;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SecurityEventSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}
