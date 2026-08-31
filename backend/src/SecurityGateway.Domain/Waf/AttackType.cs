using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.Waf;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackType
{
    Unknown,
    SqlInjection,
    CrossSiteScripting,
    LocalFileInclusion,
    RemoteFileInclusion,
    RemoteCodeExecution,
    CommandInjection,
    PathTraversal,
    BruteForce,
    Bot,
    Scanning,
    Other
}
