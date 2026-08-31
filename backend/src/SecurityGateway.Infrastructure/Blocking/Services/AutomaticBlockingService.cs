using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Blocking.DTOs;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Infrastructure.Blocking.Services;

public sealed class AutomaticBlockingService : IAutomaticBlockingService
{
    private readonly IBlocklistRepository _blocklistRepository;
    private readonly IIpAddressRepository _ipAddressRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutomaticBlockingOptions _options;

    public AutomaticBlockingService(
        IBlocklistRepository blocklistRepository,
        IIpAddressRepository ipAddressRepository,
        IUnitOfWork unitOfWork,
        AutomaticBlockingOptions options)
    {
        _blocklistRepository = blocklistRepository;
        _ipAddressRepository = ipAddressRepository;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<BlockResultDto?> CheckAndBlockAsync(string ipAddress, int? threatScore = null, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
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

        var existing = await _blocklistRepository.GetByTypeAndValueAsync(BlocklistEntryType.Ip, ipAddress, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.IsEnabled = true;
            existing.ExpiresAt = durationMinutes.HasValue
                ? DateTimeOffset.UtcNow.AddMinutes(durationMinutes.Value)
                : null;
            existing.Reason = reason;

            await _blocklistRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var entry = new BlocklistEntry
            {
                Type = BlocklistEntryType.Ip,
                Value = ipAddress,
                Reason = reason,
                ExpiresAt = durationMinutes.HasValue
                    ? DateTimeOffset.UtcNow.AddMinutes(durationMinutes.Value)
                    : null
            };

            await _blocklistRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
}
