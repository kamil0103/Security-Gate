using System.Net;
using Microsoft.Extensions.Logging;
using SecurityGateway.Application.Cloudflare;

namespace SecurityGateway.Infrastructure.Cloudflare;

public class CloudflareIpService : ICloudflareIpService
{
    private readonly CloudflareOptions _options;
    private readonly ILogger<CloudflareIpService> _logger;
    private readonly List<CloudflareNetwork> _networks = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private static readonly List<string> DefaultIpv4Ranges = new()
    {
        "173.245.48.0/20",
        "103.21.244.0/22",
        "103.22.200.0/22",
        "103.31.4.0/22",
        "141.101.64.0/18",
        "108.162.192.0/18",
        "190.93.240.0/20",
        "188.114.96.0/20",
        "197.234.240.0/22",
        "198.41.128.0/17",
        "162.158.0.0/15",
        "104.16.0.0/13",
        "104.24.0.0/14",
        "172.64.0.0/13",
        "131.0.72.0/22"
    };

    public CloudflareIpService(CloudflareOptions options, ILogger<CloudflareIpService> logger)
    {
        _options = options;
        _logger = logger;
        LoadRanges(options.IpRanges.Any() ? options.IpRanges : DefaultIpv4Ranges);
    }

    public bool IsCloudflareIp(string ipAddress)
    {
        if (!_options.Enabled || !IPAddress.TryParse(ipAddress, out var ip))
        {
            return false;
        }

        _lock.EnterReadLock();
        try
        {
            return _networks.Any(n => n.Contains(ip));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task RefreshRangesAsync(CancellationToken cancellationToken = default)
    {
        // Future: fetch from https://www.cloudflare.com/ips-v4 and ips-v6.
        _logger.LogInformation("Cloudflare IP ranges refresh requested; using configured/default ranges.");
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetRanges()
    {
        _lock.EnterReadLock();
        try
        {
            return _networks.Select(n => n.ToString()).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void LoadRanges(IEnumerable<string> ranges)
    {
        _lock.EnterWriteLock();
        try
        {
            _networks.Clear();

            foreach (var range in ranges)
            {
                if (CloudflareNetwork.TryParse(range, out var network))
                {
                    _networks.Add(network);
                }
                else
                {
                    _logger.LogWarning("Could not parse Cloudflare IP range: {Range}", range);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private sealed class CloudflareNetwork
    {
        private readonly IPAddress _network;
        private readonly int _prefix;
        private readonly uint _networkUint;

        private CloudflareNetwork(IPAddress network, int prefix, uint networkUint)
        {
            _network = network;
            _prefix = prefix;
            _networkUint = networkUint;
        }

        public static bool TryParse(string range, out CloudflareNetwork result)
        {
            result = null!;
            var parts = range.Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ip) || !int.TryParse(parts[1], out var prefix))
            {
                return false;
            }

            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return false;
            }

            var bytes = ip.GetAddressBytes();
            var networkUint = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            result = new CloudflareNetwork(ip, prefix, networkUint);
            return true;
        }

        public bool Contains(IPAddress ip)
        {
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return false;
            }

            var bytes = ip.GetAddressBytes();
            var ipUint = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            var mask = _prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - _prefix);
            return (ipUint & mask) == (_networkUint & mask);
        }

        public override string ToString() => $"{_network}/{_prefix}";
    }
}
