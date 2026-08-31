using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.Waf;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}
