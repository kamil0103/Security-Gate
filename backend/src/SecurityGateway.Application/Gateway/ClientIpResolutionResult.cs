namespace SecurityGateway.Application.Gateway;

public sealed record ClientIpResolutionResult
{
    public required string ClientIp { get; init; }
    public required IReadOnlyList<string> ProxyChain { get; init; }
    public required bool IsTrusted { get; init; }
}
