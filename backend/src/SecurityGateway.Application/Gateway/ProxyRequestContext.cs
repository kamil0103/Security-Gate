namespace SecurityGateway.Application.Gateway;

public sealed record ProxyRequestContext
{
    public required string Method { get; init; }
    public required string Path { get; init; }
    public required string QueryString { get; init; }
    public required string Host { get; init; }
    public required IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; init; }
    public required Stream? Body { get; init; }
    public required string? ClientIp { get; init; }
}
