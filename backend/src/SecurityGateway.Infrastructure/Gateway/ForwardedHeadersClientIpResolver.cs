using System.Net;
using SecurityGateway.Application.Gateway;

namespace SecurityGateway.Infrastructure.Gateway;

public sealed class ForwardedHeadersClientIpResolver : IClientIpResolver
{
    private readonly IReadOnlyList<TrustedNetwork> _trustedProxies;

    public ForwardedHeadersClientIpResolver(IEnumerable<string> trustedProxies)
    {
        _trustedProxies = trustedProxies
            .SelectMany(ParseTrustedProxy)
            .ToList()
            .AsReadOnly();
    }

    public ClientIpResolutionResult Resolve(ClientIpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var remoteIp = ParseIp(context.RemoteIp);
        var proxyChain = new List<string>();
        var isTrusted = remoteIp is not null && IsTrustedProxy(remoteIp);

        if (!isTrusted)
        {
            // The direct connection is not a trusted proxy. Use it as the client IP
            // and do not trust forwarded headers.
            return new ClientIpResolutionResult
            {
                ClientIp = context.RemoteIp ?? "unknown",
                ProxyChain = proxyChain.AsReadOnly(),
                IsTrusted = false
            };
        }

        proxyChain.Add(context.RemoteIp!);

        var forwardedFor = ParseForwardedFor(context.ForwardedFor);

        if (forwardedFor.Count > 0)
        {
            // X-Forwarded-For: client, proxy1, proxy2, ... where the rightmost is closest to the server.
            // Walk from right to left. The first untrusted IP is the client.
            for (var i = forwardedFor.Count - 1; i >= 0; i--)
            {
                var ip = forwardedFor[i];
                if (!IsTrustedProxy(ip))
                {
                    proxyChain.Add(ip.ToString());
                    return BuildResult(ip, proxyChain);
                }

                proxyChain.Add(ip.ToString());
            }

            // All proxies were trusted; the leftmost IP is the client.
            return BuildResult(forwardedFor[0], proxyChain);
        }

        if (context.RealIp.Count > 0 && TryParseIp(context.RealIp[0], out var realIp))
        {
            proxyChain.Add(realIp.ToString());
            return BuildResult(realIp, proxyChain);
        }

        return new ClientIpResolutionResult
        {
            ClientIp = context.RemoteIp ?? "unknown",
            ProxyChain = proxyChain.AsReadOnly(),
            IsTrusted = true
        };
    }

    private static ClientIpResolutionResult BuildResult(IPAddress clientIp, List<string> proxyChain)
    {
        return new ClientIpResolutionResult
        {
            ClientIp = clientIp.ToString(),
            ProxyChain = proxyChain.AsReadOnly(),
            IsTrusted = true
        };
    }

    private static IReadOnlyList<IPAddress> ParseForwardedFor(IEnumerable<string> values)
    {
        var result = new List<IPAddress>();

        foreach (var value in values)
        {
            foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParseIp(part, out var ip))
                {
                    result.Add(ip);
                }
            }
        }

        return result.AsReadOnly();
    }

    private static IEnumerable<TrustedNetwork> ParseTrustedProxy(string value)
    {
        foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseIp(part, out var ip))
            {
                yield return new TrustedNetwork(ip, null);
                continue;
            }

            if (TryParseCidr(part, out var network, out var prefixLength))
            {
                yield return new TrustedNetwork(network, prefixLength);
            }
        }
    }

    private bool IsTrustedProxy(IPAddress ip)
    {
        foreach (var trusted in _trustedProxies)
        {
            if (trusted.Contains(ip))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseIp(string value, out IPAddress ip)
    {
        return IPAddress.TryParse(value.Trim(), out ip!);
    }

    private static IPAddress? ParseIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return IPAddress.TryParse(value.Trim(), out var ip) ? ip : null;
    }

    private static bool TryParseCidr(string value, out IPAddress network, out int prefixLength)
    {
        network = IPAddress.None;
        prefixLength = 0;

        var parts = value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out prefixLength))
        {
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out var parsedNetwork))
        {
            return false;
        }

        network = parsedNetwork;
        return true;
    }

    private sealed record TrustedNetwork(IPAddress Network, int? PrefixLength)
    {
        public bool Contains(IPAddress ip)
        {
            if (ip.AddressFamily != Network.AddressFamily)
            {
                return false;
            }

            if (!PrefixLength.HasValue)
            {
                return ip.Equals(Network);
            }

            var ipBytes = ip.GetAddressBytes();
            var networkBytes = Network.GetAddressBytes();
            var bits = PrefixLength.Value;

            for (var i = 0; i < ipBytes.Length && bits > 0; i++)
            {
                var maskBits = Math.Min(8, bits);
                var mask = (byte)(0xFF << (8 - maskBits));

                if ((ipBytes[i] & mask) != (networkBytes[i] & mask))
                {
                    return false;
                }

                bits -= maskBits;
            }

            return true;
        }
    }
}
