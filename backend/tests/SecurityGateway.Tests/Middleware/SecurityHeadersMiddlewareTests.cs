using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecurityGateway.Api.Middleware;
using Xunit;

namespace SecurityGateway.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        var nextInvoked = false;

        var middleware = new SecurityHeadersMiddleware(
            _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            },
            NullLogger<SecurityHeadersMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions);
        Assert.Equal("DENY", context.Response.Headers.XFrameOptions);
        Assert.Equal("1; mode=block", context.Response.Headers.XXSSProtection);
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'", context.Response.Headers["Content-Security-Policy"]);
    }
}
