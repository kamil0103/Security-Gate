using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.AccessControl.Models;
using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl;

public interface IAccessControlService
{
    Task<bool> IsIpTrustedAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> IsBlockedAsync(string ipAddress, Guid? deviceId, Guid? userId, CancellationToken cancellationToken = default);
    Task<DeviceTrustResult> EvaluateDeviceTrustAsync(Guid userId, Guid deviceId, string ipAddress, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrustedNetworkDto>> GetTrustedNetworksAsync(CancellationToken cancellationToken = default);
    Task<TrustedNetworkDto?> GetTrustedNetworkByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TrustedNetworkDto> CreateTrustedNetworkAsync(CreateTrustedNetworkRequest request, CancellationToken cancellationToken = default);
    Task<TrustedNetworkDto> UpdateTrustedNetworkAsync(Guid id, UpdateTrustedNetworkRequest request, CancellationToken cancellationToken = default);
    Task DeleteTrustedNetworkAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlocklistEntryDto>> GetBlocklistEntriesAsync(CancellationToken cancellationToken = default);
    Task<BlocklistEntryDto?> GetBlocklistEntryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BlocklistEntryDto> CreateBlocklistEntryAsync(CreateBlocklistEntryRequest request, Guid? createdByUserId = null, CancellationToken cancellationToken = default);
    Task<BlocklistEntryDto> UpdateBlocklistEntryAsync(Guid id, CreateBlocklistEntryRequest request, CancellationToken cancellationToken = default);
    Task DeleteBlocklistEntryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AccessDecisionDto> ApproveDeviceAsync(Guid deviceId, Guid adminUserId, string? reason = null, CancellationToken cancellationToken = default);
    Task<AccessDecisionDto> DenyDeviceAsync(Guid deviceId, Guid adminUserId, string? reason = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessDecisionDto>> GetDecisionsForTargetAsync(AccessDecisionType type, Guid targetId, CancellationToken cancellationToken = default);
}
