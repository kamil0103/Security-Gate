using System.Net;
using SecurityGateway.Application.Gateway;

namespace SecurityGateway.Api.Middleware;

public sealed class GatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IProxyService _proxyService;
    private readonly IClientIpResolver _clientIpResolver;
    private readonly GatewayOptions _options;
    private readonly ILogger<GatewayMiddleware> _logger;

    public GatewayMiddleware(
        RequestDelegate next,
        IProxyService proxyService,
        IClientIpResolver clientIpResolver,
        GatewayOptions options,
        ILogger<GatewayMiddleware> logger)
    {
        _next = next;
        _proxyService = proxyService;
        _clientIpResolver = clientIpResolver;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (IsAdminPath(path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var clientIpResult = _clientIpResolver.Resolve(BuildClientIpContext(context));

        _logger.LogInformation(
            "Gateway request {Method} {Path}{QueryString} from {ClientIp} (trusted: {IsTrusted}, chain: {ProxyChain})",
            context.Request.Method,
            path,
            context.Request.QueryString.Value,
            clientIpResult.ClientIp,
            clientIpResult.IsTrusted,
            string.Join(" -> ", clientIpResult.ProxyChain));

        var proxyRequest = new ProxyRequestContext
        {
            Method = context.Request.Method,
            Path = path,
            QueryString = context.Request.QueryString.Value ?? string.Empty,
            Headers = context.Request.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.Where(v => v is not null).Cast<string>().AsEnumerable(),
                StringComparer.OrdinalIgnoreCase),
            Body = context.Request.Body,
            ClientIp = clientIpResult.ClientIp
        };

        using var proxyResponse = await _proxyService.ForwardAsync(proxyRequest, context.RequestAborted).ConfigureAwait(false);

        context.Response.StatusCode = proxyResponse.StatusCode;

        foreach (var (name, values) in proxyResponse.Headers)
        {
            context.Response.Headers[name] = values.ToArray();
        }

        await proxyResponse.Body.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
    }

    private bool IsAdminPath(string path)
    {
        foreach (var prefix in _options.AdminPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static ClientIpContext BuildClientIpContext(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        return new ClientIpContext
        {
            RemoteIp = remoteIp,
            ForwardedFor = context.Request.Headers.GetCommaSeparatedValues("X-Forwarded-For"),
            RealIp = context.Request.Headers.GetCommaSeparatedValues("X-Real-IP"),
            Forwarded = context.Request.Headers.GetCommaSeparatedValues("Forwarded")
        };
    }
}

internal static class HeaderExtensions
{
    public static IReadOnlyList<string> GetCommaSeparatedValues(this IHeaderDictionary headers, string name)
    {
        if (!headers.TryGetValue(name, out var values))
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();

        foreach (var value in values)
        {
            var text = value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (var part in text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(part);
            }
        }

        return result.AsReadOnly();
    }
}
