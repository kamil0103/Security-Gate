using System.Net;
using SecurityGateway.Application.Cloudflare;
using SecurityGateway.Application.Gateway;

namespace SecurityGateway.Infrastructure.Cloudflare;

public sealed class CloudflareClientIpResolver : IClientIpResolver
{
    private readonly IClientIpResolver _inner;
    private readonly ICloudflareIpService _cloudflareIpService;
    private readonly CloudflareOptions _options;

    public CloudflareClientIpResolver(IClientIpResolver inner, ICloudflareIpService cloudflareIpService, CloudflareOptions options)
    {
        _inner = inner;
        _cloudflareIpService = cloudflareIpService;
        _options = options;
    }

    public ClientIpResolutionResult Resolve(ClientIpContext context)
    {
        var result = _inner.Resolve(context);

        if (!_options.Enabled)
        {
            return result;
        }

        if (!_cloudflareIpService.IsCloudflareIp(context.RemoteIp ?? string.Empty))
        {
            return result;
        }

        string? cfIp = null;

        if (_options.TrustConnectingIp && context.AdditionalHeaders.TryGetValue("CF-Connecting-IP", out var values) && values.Count > 0)
        {
            cfIp = values[0];
        }
        else if (_options.TrustVisitorIp && context.AdditionalHeaders.TryGetValue("CF-Visitor-IP", out var visitorValues) && visitorValues.Count > 0)
        {
            cfIp = visitorValues[0];
        }

        if (!string.IsNullOrWhiteSpace(cfIp) && IPAddress.TryParse(cfIp, out _))
        {
            var proxyChain = new List<string>(result.ProxyChain) { result.ClientIp };

            return new ClientIpResolutionResult
            {
                ClientIp = cfIp.Trim(),
                ProxyChain = proxyChain.AsReadOnly(),
                IsTrusted = true
            };
        }

        return result;
    }
}
