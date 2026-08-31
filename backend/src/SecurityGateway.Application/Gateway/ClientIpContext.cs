namespace SecurityGateway.Application.Gateway;

public sealed record ClientIpContext
{
    public required string? RemoteIp { get; init; }
    public required IReadOnlyList<string> ForwardedFor { get; init; }
    public required IReadOnlyList<string> RealIp { get; init; }
    public required IReadOnlyList<string> Forwarded { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AdditionalHeaders { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}
