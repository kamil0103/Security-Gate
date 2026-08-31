using Microsoft.Extensions.Logging.Abstractions;
using SecurityGateway.Application.Cloudflare;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Infrastructure.Cloudflare;
using SecurityGateway.Infrastructure.Gateway;
using Xunit;

namespace SecurityGateway.Tests.Cloudflare;

public class CloudflareClientIpResolverTests
{
    private static IClientIpResolver CreateInnerResolver()
    {
        return new ForwardedHeadersClientIpResolver(["127.0.0.1"]);
    }

    private static CloudflareClientIpResolver CreateResolver(bool enabled = true, bool trustConnectingIp = true, bool trustVisitorIp = false)
    {
        var options = new CloudflareOptions
        {
            Enabled = enabled,
            TrustConnectingIp = trustConnectingIp,
            TrustVisitorIp = trustVisitorIp
        };

        var cloudflareService = new CloudflareIpService(options, NullLogger<CloudflareIpService>.Instance);
        var inner = CreateInnerResolver();
        return new CloudflareClientIpResolver(inner, cloudflareService, options);
    }

    [Fact]
    public void Resolve_Disabled_ReturnsInnerResult()
    {
        var resolver = CreateResolver(enabled: false);
        var context = new ClientIpContext
        {
            RemoteIp = "104.16.0.1",
            ForwardedFor = [],
            RealIp = [],
            Forwarded = [],
            AdditionalHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CF-Connecting-IP"] = ["198.51.100.10"]
            }
        };

        var result = resolver.Resolve(context);

        Assert.Equal("104.16.0.1", result.ClientIp);
        Assert.False(result.IsTrusted);
    }

    [Fact]
    public void Resolve_NotCloudflareIp_ReturnsInnerResult()
    {
        var resolver = CreateResolver();
        var context = new ClientIpContext
        {
            RemoteIp = "203.0.113.1",
            ForwardedFor = [],
            RealIp = [],
            Forwarded = [],
            AdditionalHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CF-Connecting-IP"] = ["198.51.100.10"]
            }
        };

        var result = resolver.Resolve(context);

        Assert.Equal("203.0.113.1", result.ClientIp);
        Assert.False(result.IsTrusted);
    }

    [Fact]
    public void Resolve_CloudflareConnectingIp_ReturnsCfIp()
    {
        var resolver = CreateResolver();
        var context = new ClientIpContext
        {
            RemoteIp = "104.16.0.1",
            ForwardedFor = [],
            RealIp = [],
            Forwarded = [],
            AdditionalHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CF-Connecting-IP"] = ["198.51.100.10"]
            }
        };

        var result = resolver.Resolve(context);

        Assert.Equal("198.51.100.10", result.ClientIp);
        Assert.True(result.IsTrusted);
        Assert.Contains("104.16.0.1", result.ProxyChain);
    }

    [Fact]
    public void Resolve_CloudflareVisitorIp_WhenConnectingIpDisabled_ReturnsVisitorIp()
    {
        var resolver = CreateResolver(trustConnectingIp: false, trustVisitorIp: true);
        var context = new ClientIpContext
        {
            RemoteIp = "104.16.0.1",
            ForwardedFor = [],
            RealIp = [],
            Forwarded = [],
            AdditionalHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CF-Visitor-IP"] = ["198.51.100.20"]
            }
        };

        var result = resolver.Resolve(context);

        Assert.Equal("198.51.100.20", result.ClientIp);
        Assert.True(result.IsTrusted);
    }

    [Fact]
    public void Resolve_CloudflareMissingHeader_ReturnsInnerResult()
    {
        var resolver = CreateResolver();
        var context = new ClientIpContext
        {
            RemoteIp = "104.16.0.1",
            ForwardedFor = [],
            RealIp = [],
            Forwarded = [],
            AdditionalHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        };

        var result = resolver.Resolve(context);

        Assert.Equal("104.16.0.1", result.ClientIp);
        Assert.False(result.IsTrusted);
    }
}
