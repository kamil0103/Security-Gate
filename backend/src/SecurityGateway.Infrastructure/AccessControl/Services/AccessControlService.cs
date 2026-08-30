using System.Net;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.AccessControl.Models;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.ThreatDetection.DTOs;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Infrastructure.AccessControl.Services;

public sealed class AccessControlService : IAccessControlService
{
    private readonly ITrustedNetworkRepository _trustedNetworkRepository;
    private readonly IBlocklistRepository _blocklistRepository;
    private readonly IAccessDecisionRepository _accessDecisionRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IThreatDetectionService _threatDetectionService;
    private readonly IUnitOfWork _unitOfWork;

    public AccessControlService(
        ITrustedNetworkRepository trustedNetworkRepository,
        IBlocklistRepository blocklistRepository,
        IAccessDecisionRepository accessDecisionRepository,
        IDeviceRepository deviceRepository,
        IThreatDetectionService threatDetectionService,
        IUnitOfWork unitOfWork)
    {
        _trustedNetworkRepository = trustedNetworkRepository;
        _blocklistRepository = blocklistRepository;
        _accessDecisionRepository = accessDecisionRepository;
        _deviceRepository = deviceRepository;
        _threatDetectionService = threatDetectionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> IsIpTrustedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
        {
            return false;
        }

        var networks = await _trustedNetworkRepository.GetEnabledAsync(cancellationToken).ConfigureAwait(false);
        return networks.Any(n => IsIpInNetwork(ip, n.Cidr));
    }

    public async Task<bool> IsBlockedAsync(string ipAddress, Guid? deviceId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entries = await _blocklistRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);

        foreach (var entry in entries)
        {
            if (IsMatch(entry, ipAddress, deviceId, userId))
            {
                await RecordBlockedEventAsync(ipAddress, deviceId, userId, entry.Type, entry.Value, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        return false;
    }

    public async Task<DeviceTrustResult> EvaluateDeviceTrustAsync(Guid userId, Guid deviceId, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (await IsBlockedAsync(ipAddress, deviceId, userId, cancellationToken).ConfigureAwait(false))
        {
            return new DeviceTrustResult
            {
                IsTrusted = false,
                IsPending = false,
                IsBlocked = true,
                Reason = "Blocked by access control policy"
            };
        }

        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false);

        if (device is null)
        {
            return new DeviceTrustResult
            {
                IsTrusted = false,
                IsPending = true,
                IsBlocked = false,
                Reason = "Device not enrolled"
            };
        }

        if (device.TrustStatus == DeviceTrustStatus.Blocked)
        {
            return new DeviceTrustResult
            {
                IsTrusted = false,
                IsPending = false,
                IsBlocked = true,
                Reason = "Device is blocked"
            };
        }

        if (device.TrustStatus == DeviceTrustStatus.Trusted)
        {
            return new DeviceTrustResult
            {
                IsTrusted = true,
                IsPending = false,
                IsBlocked = false,
                Reason = "Device is trusted"
            };
        }

        var isTrustedNetwork = await IsIpTrustedAsync(ipAddress, cancellationToken).ConfigureAwait(false);

        if (isTrustedNetwork && device.TrustStatus == DeviceTrustStatus.Pending)
        {
            device.TrustStatus = DeviceTrustStatus.Trusted;
            await _deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new DeviceTrustResult
            {
                IsTrusted = true,
                IsPending = false,
                IsBlocked = false,
                Reason = "Auto-approved from trusted network"
            };
        }

        return new DeviceTrustResult
        {
            IsTrusted = false,
            IsPending = device.TrustStatus == DeviceTrustStatus.Pending,
            IsBlocked = false,
            Reason = device.TrustStatus == DeviceTrustStatus.Pending ? "Device pending approval" : "Device is untrusted"
        };
    }

    public async Task<IReadOnlyList<TrustedNetworkDto>> GetTrustedNetworksAsync(CancellationToken cancellationToken = default)
    {
        var networks = await _trustedNetworkRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return networks.Select(MapTrustedNetwork).ToList().AsReadOnly();
    }

    public async Task<TrustedNetworkDto?> GetTrustedNetworkByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var network = await _trustedNetworkRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return network is null ? null : MapTrustedNetwork(network);
    }

    public async Task<TrustedNetworkDto> CreateTrustedNetworkAsync(CreateTrustedNetworkRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Cidr) || !IsValidCidr(request.Cidr))
        {
            throw new ArgumentException("A valid CIDR is required.", nameof(request));
        }

        var existing = await _trustedNetworkRepository.GetByCidrAsync(request.Cidr, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            throw new InvalidOperationException("A trusted network with this CIDR already exists.");
        }

        var network = new TrustedNetwork
        {
            Name = request.Name,
            Cidr = request.Cidr,
            Description = request.Description
        };

        await _trustedNetworkRepository.AddAsync(network, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapTrustedNetwork(network);
    }

    public async Task<TrustedNetworkDto> UpdateTrustedNetworkAsync(Guid id, UpdateTrustedNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var network = await _trustedNetworkRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Trusted network not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Cidr) || !IsValidCidr(request.Cidr))
        {
            throw new ArgumentException("A valid CIDR is required.", nameof(request));
        }

        network.Name = request.Name;
        network.Cidr = request.Cidr;
        network.Description = request.Description;
        network.IsEnabled = request.IsEnabled;

        await _trustedNetworkRepository.UpdateAsync(network, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapTrustedNetwork(network);
    }

    public async Task DeleteTrustedNetworkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var network = await _trustedNetworkRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Trusted network not found.");

        await _trustedNetworkRepository.DeleteAsync(network, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BlocklistEntryDto>> GetBlocklistEntriesAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _blocklistRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return entries.Select(MapBlocklistEntry).ToList().AsReadOnly();
    }

    public async Task<BlocklistEntryDto?> GetBlocklistEntryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _blocklistRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return entry is null ? null : MapBlocklistEntry(entry);
    }

    public async Task<BlocklistEntryDto> CreateBlocklistEntryAsync(CreateBlocklistEntryRequest request, Guid? createdByUserId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
        {
            throw new ArgumentException("Value is required.", nameof(request));
        }

        var existing = await _blocklistRepository.GetByTypeAndValueAsync(request.Type, request.Value, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            throw new InvalidOperationException("A blocklist entry with this type and value already exists.");
        }

        var entry = new BlocklistEntry
        {
            Type = request.Type,
            Value = request.Value,
            Reason = request.Reason,
            ExpiresAt = request.ExpiresAt,
            CreatedByUserId = createdByUserId
        };

        await _blocklistRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapBlocklistEntry(entry);
    }

    public async Task<BlocklistEntryDto> UpdateBlocklistEntryAsync(Guid id, CreateBlocklistEntryRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await _blocklistRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Blocklist entry not found.");

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            throw new ArgumentException("Value is required.", nameof(request));
        }

        entry.Reason = request.Reason;
        entry.ExpiresAt = request.ExpiresAt;

        await _blocklistRepository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapBlocklistEntry(entry);
    }

    public async Task DeleteBlocklistEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _blocklistRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Blocklist entry not found.");

        await _blocklistRepository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AccessDecisionDto> ApproveDeviceAsync(Guid deviceId, Guid adminUserId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Device not found.");

        device.TrustStatus = DeviceTrustStatus.Trusted;
        await _deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);

        var decision = new AccessDecision
        {
            Type = AccessDecisionType.DeviceApproval,
            TargetId = deviceId,
            Outcome = AccessDecisionOutcome.Approved,
            Reason = reason,
            CreatedByUserId = adminUserId
        };

        await _accessDecisionRepository.AddAsync(decision, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapAccessDecision(decision);
    }

    public async Task<AccessDecisionDto> DenyDeviceAsync(Guid deviceId, Guid adminUserId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Device not found.");

        device.TrustStatus = DeviceTrustStatus.Untrusted;
        await _deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);

        var decision = new AccessDecision
        {
            Type = AccessDecisionType.DeviceApproval,
            TargetId = deviceId,
            Outcome = AccessDecisionOutcome.Denied,
            Reason = reason,
            CreatedByUserId = adminUserId
        };

        await _accessDecisionRepository.AddAsync(decision, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapAccessDecision(decision);
    }

    public async Task<IReadOnlyList<AccessDecisionDto>> GetDecisionsForTargetAsync(AccessDecisionType type, Guid targetId, CancellationToken cancellationToken = default)
    {
        var decisions = await _accessDecisionRepository.GetByTargetAsync(type, targetId, cancellationToken).ConfigureAwait(false);
        return decisions.Select(MapAccessDecision).ToList().AsReadOnly();
    }

    private async Task RecordBlockedEventAsync(string ipAddress, Guid? deviceId, Guid? userId, BlocklistEntryType type, string value, CancellationToken cancellationToken)
    {
        try
        {
            await _threatDetectionService.RecordEventAsync(new CreateSecurityEventRequest
            {
                Type = SecurityEventType.AccessBlocked,
                Severity = SecurityEventSeverity.High,
                SourceIp = ipAddress,
                UserId = userId,
                DeviceId = deviceId,
                Description = $"Access blocked by {type} blocklist entry: {value}",
                RelatedEntityType = "BlocklistEntry",
                RelatedEntityId = value
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort threat detection.
        }
    }

    private static bool IsMatch(BlocklistEntry entry, string ipAddress, Guid? deviceId, Guid? userId)
    {
        if (!entry.IsEnabled)
        {
            return false;
        }

        if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        return entry.Type switch
        {
            BlocklistEntryType.Ip => entry.Value.Equals(ipAddress, StringComparison.OrdinalIgnoreCase),
            BlocklistEntryType.Network => IPAddress.TryParse(ipAddress, out var ip) && IsIpInNetwork(ip, entry.Value),
            BlocklistEntryType.Device => deviceId.HasValue && entry.Value.Equals(deviceId.Value.ToString(), StringComparison.OrdinalIgnoreCase),
            BlocklistEntryType.User => userId.HasValue && entry.Value.Equals(userId.Value.ToString(), StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool IsIpInNetwork(IPAddress ip, string cidr)
    {
        if (!IPAddress.TryParse(cidr.Split('/')[0], out var networkIp))
        {
            return false;
        }

        if (!int.TryParse(cidr.Split('/')[1], out var prefixLength))
        {
            return false;
        }

        var ipBytes = ip.GetAddressBytes();
        var networkBytes = networkIp.GetAddressBytes();

        if (ipBytes.Length != networkBytes.Length)
        {
            return false;
        }

        var mask = prefixLength == 0 ? 0 : uint.MaxValue << (32 - prefixLength);

        uint IpToUint(byte[] bytes) => ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

        return (IpToUint(ipBytes) & mask) == (IpToUint(networkBytes) & mask);
    }

    private static bool IsValidCidr(string cidr)
    {
        var parts = cidr.Split('/');

        if (parts.Length != 2)
        {
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out _))
        {
            return false;
        }

        return int.TryParse(parts[1], out var prefix) && prefix is >= 0 and <= 32;
    }

    private static TrustedNetworkDto MapTrustedNetwork(TrustedNetwork network)
    {
        return new TrustedNetworkDto
        {
            Id = network.Id,
            Name = network.Name,
            Cidr = network.Cidr,
            Description = network.Description,
            IsEnabled = network.IsEnabled,
            CreatedAt = network.CreatedAt
        };
    }

    private static BlocklistEntryDto MapBlocklistEntry(BlocklistEntry entry)
    {
        return new BlocklistEntryDto
        {
            Id = entry.Id,
            Type = entry.Type,
            Value = entry.Value,
            Reason = entry.Reason,
            ExpiresAt = entry.ExpiresAt,
            IsEnabled = entry.IsEnabled,
            CreatedByUserId = entry.CreatedByUserId,
            CreatedAt = entry.CreatedAt
        };
    }

    private static AccessDecisionDto MapAccessDecision(AccessDecision decision)
    {
        return new AccessDecisionDto
        {
            Id = decision.Id,
            Type = decision.Type,
            TargetId = decision.TargetId,
            Outcome = decision.Outcome,
            Reason = decision.Reason,
            CreatedByUserId = decision.CreatedByUserId,
            CreatedAt = decision.CreatedAt
        };
    }
}
