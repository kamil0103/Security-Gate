namespace SecurityGateway.Application.Gateway;

public interface IProxyService
{
    Task<ProxyResponse> ForwardAsync(ProxyRequestContext request, string? upstreamUrl = null, CancellationToken cancellationToken = default);
}
