using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl;

public interface ITrustRecordRepository
{
    Task<IReadOnlyList<TrustRecord>> FindActiveAsync(Guid applicationId, string clientIp, string? deviceFingerprint, Guid? userId, string? sessionId, CancellationToken cancellationToken = default);
    Task AddAsync(TrustRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(TrustRecord record, CancellationToken cancellationToken = default);
}
