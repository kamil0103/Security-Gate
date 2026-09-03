using System.Net;

namespace SecurityGateway.Application.Gateway;

public sealed class ProxyResponse : IDisposable
{
    public required int StatusCode { get; init; }
    public required IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; init; }
    public required Stream Body { get; init; }

    /// <summary>
    /// The upstream <see cref="HttpResponseMessage"/> that owns the response stream.
    /// Keeping this alive until the proxy response is disposed prevents socket exhaustion.
    /// </summary>
    public HttpResponseMessage? UpstreamResponse { get; init; }

    public void Dispose()
    {
        try
        {
            Body.Dispose();
        }
        catch
        {
            // Best-effort cleanup.
        }

        UpstreamResponse?.Dispose();
    }
}
