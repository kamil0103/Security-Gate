using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecurityGateway.Api.Middleware;
using SecurityGateway.Application.Gateway;
using Xunit;

namespace SecurityGateway.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AdminHost_AddsStrictSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("admin.example.com");
        var nextInvoked = false;

        var options = new GatewayOptions { AdminDomain = "admin.example.com" };
        var middleware = new SecurityHeadersMiddleware(
            _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            },
            NullLogger<SecurityHeadersMiddleware>.Instance,
            options);

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions);
        Assert.Equal("DENY", context.Response.Headers.XFrameOptions);
        Assert.Equal("1; mode=block", context.Response.Headers.XXSSProtection);
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        Assert.Contains("frame-ancestors 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ProxiedHost_DoesNotSetBlanketCsp()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("app.example.com");
        var nextInvoked = false;

        var options = new GatewayOptions { AdminDomain = "admin.example.com" };
        var middleware = new SecurityHeadersMiddleware(
            _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            },
            NullLogger<SecurityHeadersMiddleware>.Instance,
            options);

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("SAMEORIGIN", context.Response.Headers.XFrameOptions);
    }
}
