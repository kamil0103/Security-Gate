namespace SecurityGateway.Application.Gateway;

public sealed record ProxyResponse : IDisposable
{
    public required int StatusCode { get; init; }
    public required IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; init; }
    public required Stream Body { get; init; }

    public void Dispose()
    {
        Body.Dispose();
    }
}
