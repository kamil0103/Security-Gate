using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecurityGateway.Api.Middleware;
using SecurityGateway.Application.Gateway;
using Xunit;

namespace SecurityGateway.Tests.Gateway;

public class GatewayMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AdminPath_CallsNextAndDoesNotProxy()
    {
        var proxyService = new FakeProxyService();
        var resolver = new FakeClientIpResolver();
        var options = new GatewayOptions { AdminPathPrefixes = ["/api"] };
        var nextInvoked = false;

        var middleware = new GatewayMiddleware(
            _ => { nextInvoked = true; return Task.CompletedTask; },
            proxyService,
            resolver,
            options,
            NullLogger<GatewayMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/health";

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.False(proxyService.WasCalled);
    }

    [Fact]
    public async Task InvokeAsync_ProxiedPath_ForwardsRequest()
    {
        var proxyService = new FakeProxyService();
        var resolver = new FakeClientIpResolver { Result = new ClientIpResolutionResult { ClientIp = "198.51.100.1", ProxyChain = [], IsTrusted = true } };
        var options = new GatewayOptions { AdminPathPrefixes = ["/api"] };

        var middleware = new GatewayMiddleware(
            _ => Task.CompletedTask,
            proxyService,
            resolver,
            options,
            NullLogger<GatewayMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/immich";
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?id=1");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.True(proxyService.WasCalled);
        Assert.Equal("/immich", proxyService.LastRequest?.Path);
        Assert.Equal("?id=1", proxyService.LastRequest?.QueryString);
        Assert.Equal("198.51.100.1", proxyService.LastRequest?.ClientIp);
        Assert.Equal(200, context.Response.StatusCode);
    }

    private sealed class FakeProxyService : IProxyService
    {
        public bool WasCalled { get; private set; }
        public ProxyRequestContext? LastRequest { get; private set; }

        public Task<ProxyResponse> ForwardAsync(ProxyRequestContext request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastRequest = request;

            return Task.FromResult(new ProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase),
                Body = new MemoryStream()
            });
        }
    }

    private sealed class FakeClientIpResolver : IClientIpResolver
    {
        public ClientIpResolutionResult Result { get; set; } = new()
        {
            ClientIp = "127.0.0.1",
            ProxyChain = [],
            IsTrusted = true
        };

        public ClientIpResolutionResult Resolve(ClientIpContext context) => Result;
    }
}
