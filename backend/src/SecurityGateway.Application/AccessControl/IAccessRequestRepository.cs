using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl;

public interface IAccessRequestRepository
{
    Task<AccessRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccessRequest?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    Task<AccessRequest?> FindPendingAsync(Guid applicationId, string clientIp, string? deviceFingerprint, string? sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRequest>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRequest>> GetByIpAsync(string ip, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRequest>> GetByDeviceFingerprintAsync(string fingerprint, int limit, CancellationToken cancellationToken = default);
    Task AddAsync(AccessRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default);
}
