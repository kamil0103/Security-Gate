using System.Net;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Blocking.DTOs;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Audit;

namespace SecurityGateway.Infrastructure.Blocking.Services;

public sealed class AutomaticBlockingService : IAutomaticBlockingService
{
    private readonly IBlocklistRepository _blocklistRepository;
    private readonly IIpAddressRepository _ipAddressRepository;
    private readonly ITrustedNetworkRepository _trustedNetworkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutomaticBlockingOptions _options;
    private readonly IAuditService _auditService;

    public AutomaticBlockingService(
        IBlocklistRepository blocklistRepository,
        IIpAddressRepository ipAddressRepository,
        ITrustedNetworkRepository trustedNetworkRepository,
        IUnitOfWork unitOfWork,
        AutomaticBlockingOptions options,
        IAuditService auditService)
    {
        _blocklistRepository = blocklistRepository;
        _ipAddressRepository = ipAddressRepository;
        _trustedNetworkRepository = trustedNetworkRepository;
        _unitOfWork = unitOfWork;
        _options = options;
        _auditService = auditService;
    }

    public async Task<BlockResultDto?> CheckAndBlockAsync(string ipAddress, int? threatScore = null, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        if (!IPAddress.TryParse(ipAddress, out var ip))
        {
            return null;
        }

        var trustedNetworks = await _trustedNetworkRepository.GetEnabledAsync(cancellationToken).ConfigureAwait(false);
        if (trustedNetworks.Any(n => IsIpInNetwork(ip, n.Cidr)))
        {
            return null;
        }

        var score = threatScore ?? await GetThreatScoreAsync(ipAddress, cancellationToken).ConfigureAwait(false);

        if (score < _options.MediumThreshold)
        {
            return null;
        }

        var (durationMinutes, reason) = score switch
        {
            int n when n >= _options.CriticalThreshold => (_options.CriticalBlockDurationMinutes, "Automatic block: critical threat score"),
            int n when n >= _options.HighThreshold => (_options.HighBlockDurationMinutes, "Automatic block: high threat score"),
            _ => (_options.MediumBlockDurationMinutes, "Automatic block: medium threat score")
        };

        var existing = await _blocklistRepository.GetByTypeAndValueAsync(BlocklistEntryType.Ip, ipAddress, cancellationToken).ConfigureAwait(false);

        if (existing is not null && existing.IsEnabled && (existing.ExpiresAt == null || existing.ExpiresAt > DateTimeOffset.UtcNow))
        {
            return new BlockResultDto
            {
                Blocked = true,
                IpAddress = ipAddress,
                ExpiresAt = existing.ExpiresAt,
                Reason = existing.Reason
            };
        }

        return await BlockAsync(ipAddress, durationMinutes, reason, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BlockResultDto> BlockAsync(string ipAddress, int? durationMinutes = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);

        var entry = new BlocklistEntry
        {
            Type = BlocklistEntryType.Ip,
            Value = ipAddress,
            Reason = reason,
            ExpiresAt = durationMinutes.HasValue
                ? DateTimeOffset.UtcNow.AddMinutes(durationMinutes.Value)
                : null
        };

        await _blocklistRepository.UpsertAsync(entry, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.Blocking,
            durationMinutes.HasValue ? "BlockIp" : "PermanentlyBlockIp",
            null,
            null,
            ipAddress,
            reason,
            true,
            cancellationToken).ConfigureAwait(false);

        return new BlockResultDto
        {
            Blocked = true,
            IpAddress = ipAddress,
            ExpiresAt = durationMinutes.HasValue ? DateTimeOffset.UtcNow.AddMinutes(durationMinutes.Value) : null,
            Reason = reason
        };
    }

    public async Task UnblockAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var existing = await _blocklistRepository.GetByTypeAndValueAsync(BlocklistEntryType.Ip, ipAddress, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        await _blocklistRepository.DeleteAsync(existing, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.Blocking,
            "UnblockIp",
            null,
            null,
            ipAddress,
            "IP manually unblocked",
            true,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsBlockedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var entries = await _blocklistRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        return entries.Any(e => e.Type == BlocklistEntryType.Ip && e.Value.Equals(ipAddress, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<int> GetThreatScoreAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var ip = await _ipAddressRepository.GetByIpAsync(ipAddress, cancellationToken).ConfigureAwait(false);
        return ip?.ThreatScore ?? 0;
    }

    private static bool IsIpInNetwork(IPAddress ip, string cidr)
    {
        if (!IPAddress.TryParse(cidr.Split('/')[0], out var networkAddress))
        {
            return false;
        }

        var prefixLength = cidr.Contains('/') && int.TryParse(cidr.Split('/')[1], out var length)
            ? length
            : (networkAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128);

        if (ip.AddressFamily != networkAddress.AddressFamily)
        {
            return false;
        }

        var ipBytes = ip.GetAddressBytes();
        var networkBytes = networkAddress.GetAddressBytes();

        for (int i = 0; i < ipBytes.Length; i++)
        {
            var bitsInByte = Math.Min(8, prefixLength - (i * 8));
            if (bitsInByte <= 0)
            {
                break;
            }

            var mask = (byte)(0xFF << (8 - bitsInByte));
            if ((ipBytes[i] & mask) != (networkBytes[i] & mask))
            {
                return false;
            }
        }

        return true;
    }
}
