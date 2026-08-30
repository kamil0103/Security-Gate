namespace SecurityGateway.Application.Gateway;

public interface IClientIpResolver
{
    ClientIpResolutionResult Resolve(ClientIpContext context);
}
