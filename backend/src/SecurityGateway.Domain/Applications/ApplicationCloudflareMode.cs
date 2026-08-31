using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.Applications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationCloudflareMode
{
    Proxied,
    Direct
}
