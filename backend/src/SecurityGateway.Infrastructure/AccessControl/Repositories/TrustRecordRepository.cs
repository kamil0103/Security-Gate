using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.AccessControl.Repositories;

public sealed class TrustRecordRepository : ITrustRecordRepository
{
    private readonly ApplicationDbContext _context;

    public TrustRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TrustRecord>> FindActiveAsync(Guid applicationId, string clientIp, string? deviceFingerprint, Guid? userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var query = _context.TrustRecords
            .AsNoTracking()
            .Where(r => r.ApplicationId == applicationId
                        && !r.IsRevoked
                        && (r.ExpiresAt == null || r.ExpiresAt > now)
                        && (r.ClientIp == null || r.ClientIp == clientIp)
                        && (r.SessionId == null || r.SessionId == sessionId)
                        && (r.DeviceFingerprint == null || r.DeviceFingerprint == deviceFingerprint)
                        && (r.UserId == null || r.UserId == userId));

        var records = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        return records.AsReadOnly();
    }

    public async Task AddAsync(TrustRecord record, CancellationToken cancellationToken = default)
    {
        await _context.TrustRecords.AddAsync(record, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(TrustRecord record, CancellationToken cancellationToken = default)
    {
        _context.TrustRecords.Update(record);
        return Task.CompletedTask;
    }
}
