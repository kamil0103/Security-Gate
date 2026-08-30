namespace SecurityGateway.Application.Gateway;

public interface IProxyService
{
    Task<ProxyResponse> ForwardAsync(ProxyRequestContext request, CancellationToken cancellationToken = default);
}
