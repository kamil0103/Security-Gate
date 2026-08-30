using SecurityGateway.Application.Gateway;
using SecurityGateway.Infrastructure.Gateway;
using Xunit;

namespace SecurityGateway.Tests.Gateway;

public class ForwardedHeadersClientIpResolverTests
{
    private static IClientIpResolver CreateResolver(params string[] trustedProxies)
    {
        return new ForwardedHeadersClientIpResolver(trustedProxies);
    }

    [Fact]
    public void Resolve_UntrustedDirectConnection_IgnoresForwardedHeaders()
    {
        var resolver = CreateResolver("127.0.0.1");
        var context = new ClientIpContext
        {
            RemoteIp = "203.0.113.10",
            ForwardedFor = ["198.51.100.5"],
            RealIp = [],
            Forwarded = []
        };

        var result = resolver.Resolve(context);

        Assert.Equal("203.0.113.10", result.ClientIp);
        Assert.False(result.IsTrusted);
        Assert.Empty(result.ProxyChain);
    }

    [Fact]
    public void Resolve_TrustedProxyWithSingleForwardedFor_ReturnsClientIp()
    {
        var resolver = CreateResolver("127.0.0.1");
        var context = new ClientIpContext
        {
            RemoteIp = "127.0.0.1",
            ForwardedFor = ["198.51.100.5"],
            RealIp = [],
            Forwarded = []
        };

        var result = resolver.Resolve(context);

        Assert.Equal("198.51.100.5", result.ClientIp);
        Assert.True(result.IsTrusted);
        Assert.Equal(["127.0.0.1", "198.51.100.5"], result.ProxyChain);
    }

    [Fact]
    public void Resolve_MultipleProxies_WalksChainFromRightToLeft()
    {
        var resolver = CreateResolver("10.0.0.0/8", "172.16.0.0/12");
        var context = new ClientIpContext
        {
            RemoteIp = "10.0.0.1",
            ForwardedFor = ["198.51.100.5", "172.16.0.10", "10.0.0.2"],
            RealIp = [],
            Forwarded = []
        };

        var result = resolver.Resolve(context);

        Assert.Equal("198.51.100.5", result.ClientIp);
        Assert.True(result.IsTrusted);
        Assert.Equal(["10.0.0.1", "10.0.0.2", "172.16.0.10", "198.51.100.5"], result.ProxyChain);
    }

    [Fact]
    public void Resolve_AllTrusted_ReturnsLeftmostAsClient()
    {
        var resolver = CreateResolver("10.0.0.0/8");
        var context = new ClientIpContext
        {
            RemoteIp = "10.0.0.1",
            ForwardedFor = ["10.0.0.3", "10.0.0.2"],
            RealIp = [],
            Forwarded = []
        };

        var result = resolver.Resolve(context);

        Assert.Equal("10.0.0.3", result.ClientIp);
        Assert.True(result.IsTrusted);
    }

    [Fact]
    public void Resolve_NoForwardedFor_UsesRemoteIp()
    {
        var resolver = CreateResolver("127.0.0.1");
        var context = new ClientIpContext
        {
            RemoteIp = "127.0.0.1",
            ForwardedFor = [],
            RealIp = [],
            Forwarded = []
        };

        var result = resolver.Resolve(context);

        Assert.Equal("127.0.0.1", result.ClientIp);
        Assert.True(result.IsTrusted);
    }

    [Fact]
    public void Resolve_RealIpHeader_UsedWhenNoForwardedFor()
    {
        var resolver = CreateResolver("127.0.0.1");
        var context = new ClientIpContext
        {
            RemoteIp = "127.0.0.1",
            ForwardedFor = [],
            RealIp = ["198.51.100.7"],
            Forwarded = []
        };

        var result = resolver.Resolve(context);

        Assert.Equal("198.51.100.7", result.ClientIp);
        Assert.True(result.IsTrusted);
    }

    [Fact]
    public void Resolve_CidrRange_MatchesSubnet()
    {
        var resolver = CreateResolver("192.168.5.0/24");
        var context = new ClientIpContext
        {
            RemoteIp = "192.168.5.184",
            ForwardedFor = ["198.51.100.9"],
            RealIp = [],
            Forwarded = []
        };

        var result = resolver.Resolve(context);

        Assert.Equal("198.51.100.9", result.ClientIp);
        Assert.True(result.IsTrusted);
    }
}
