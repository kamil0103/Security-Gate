using System.Net;
using System.Net.Http.Headers;
using SecurityGateway.Application.Gateway;

namespace SecurityGateway.Infrastructure.Gateway;

public sealed class HttpClientProxyService : IProxyService
{
    private readonly HttpClient _httpClient;

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    public HttpClientProxyService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
    }

    public async Task<ProxyResponse> ForwardAsync(ProxyRequestContext request, string? upstreamUrl = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = BuildRequestUri(upstreamUrl, request.Path, request.QueryString);
        using var upstreamRequest = new HttpRequestMessage(new HttpMethod(request.Method), requestUri);

        foreach (var (name, values) in request.Headers)
        {
            if (ShouldSkipRequestHeader(name))
            {
                continue;
            }

            upstreamRequest.Headers.TryAddWithoutValidation(name, values);
        }

        if (request.ClientIp is not null)
        {
            upstreamRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", request.ClientIp);
            upstreamRequest.Headers.TryAddWithoutValidation("X-Real-Ip", request.ClientIp);
        }

        if (request.Body is not null)
        {
            upstreamRequest.Content = new StreamContent(request.Body);

            if (request.Headers.TryGetValue("Content-Type", out var contentTypeValues))
            {
                var contentType = contentTypeValues.FirstOrDefault();
                if (contentType is not null)
                {
                    upstreamRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                }
            }
        }

        try
        {
            var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            var responseHeaders = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in upstreamResponse.Headers)
            {
                if (!HopByHopHeaders.Contains(header.Key))
                {
                    responseHeaders[header.Key] = header.Value;
                }
            }

            foreach (var header in upstreamResponse.Content.Headers)
            {
                if (!HopByHopHeaders.Contains(header.Key))
                {
                    responseHeaders[header.Key] = header.Value;
                }
            }

            return new ProxyResponse
            {
                StatusCode = (int)upstreamResponse.StatusCode,
                Headers = responseHeaders,
                Body = await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)
            };
        }
        catch (HttpRequestException)
        {
            return new ProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadGateway,
                Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Content-Type"] = ["text/plain"]
                },
                Body = new MemoryStream("Bad Gateway: upstream is unavailable."u8.ToArray())
            };
        }
    }

    private Uri BuildRequestUri(string? upstreamUrl, string path, string queryString)
    {
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        var pathAndQuery = string.IsNullOrEmpty(queryString)
            ? normalizedPath
            : $"{normalizedPath}{queryString}";

        if (string.IsNullOrWhiteSpace(upstreamUrl))
        {
            return new Uri(_httpClient.BaseAddress!, pathAndQuery);
        }

        var baseUri = upstreamUrl.EndsWith('/')
            ? upstreamUrl
            : upstreamUrl + "/";

        return new Uri(new Uri(baseUri), pathAndQuery);
    }

    private static bool ShouldSkipRequestHeader(string name)
    {
        if (HopByHopHeaders.Contains(name))
        {
            return true;
        }

        return name.Equals("Host", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase);
    }
}
