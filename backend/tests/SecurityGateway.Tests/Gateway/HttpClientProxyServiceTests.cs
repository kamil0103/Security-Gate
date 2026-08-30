using System.Net;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Infrastructure.Gateway;
using Xunit;

namespace SecurityGateway.Tests.Gateway;

public class HttpClientProxyServiceTests
{
    [Fact]
    public async Task ForwardAsync_ReturnsUpstreamResponse()
    {
        var handler = new TestHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("proxied")
            };
            response.Headers.Add("X-Custom-Header", "value");
            return response;
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://npm") };
        var service = new HttpClientProxyService(client);

        var request = new ProxyRequestContext
        {
            Method = "GET",
            Path = "/app",
            QueryString = "?foo=bar",
            Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase),
            Body = null,
            ClientIp = "198.51.100.1"
        };

        using var response = await service.ForwardAsync(request);
        var body = await new StreamReader(response.Body).ReadToEndAsync();

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("proxied", body);
        Assert.Contains("X-Custom-Header", response.Headers);
        Assert.Equal("/app?foo=bar", handler.LastRequest?.RequestUri?.PathAndQuery);
    }

    [Fact]
    public async Task ForwardAsync_UpstreamUnavailable_ReturnsBadGateway()
    {
        var handler = new TestHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://npm") };
        var service = new HttpClientProxyService(client);

        var request = new ProxyRequestContext
        {
            Method = "GET",
            Path = "/app",
            QueryString = string.Empty,
            Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase),
            Body = null,
            ClientIp = null
        };

        using var response = await service.ForwardAsync(request);
        var body = await new StreamReader(response.Body).ReadToEndAsync();

        Assert.Equal(502, response.StatusCode);
        Assert.Contains("Bad Gateway", body);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_handler(request));
        }
    }
}
