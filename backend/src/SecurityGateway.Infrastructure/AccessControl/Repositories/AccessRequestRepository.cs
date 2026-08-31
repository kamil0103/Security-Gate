using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.AccessControl.Repositories;

public sealed class AccessRequestRepository : IAccessRequestRepository
{
    private readonly ApplicationDbContext _context;

    public AccessRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AccessRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.AccessRequests
            .AsNoTracking()
            .Include(r => r.Application)
            .Include(r => r.IpAddress)
            .Include(r => r.User)
            .Include(r => r.ReviewedByUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<AccessRequest?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        return _context.AccessRequests
            .AsNoTracking()
            .Include(r => r.Application)
            .Include(r => r.IpAddress)
            .Include(r => r.User)
            .Include(r => r.ReviewedByUser)
            .FirstOrDefaultAsync(r => r.PublicId == publicId, cancellationToken);
    }

    public Task<AccessRequest?> FindPendingAsync(Guid applicationId, string clientIp, string? deviceFingerprint, string? sessionId, CancellationToken cancellationToken = default)
    {
        var query = _context.AccessRequests
            .Where(r => r.ApplicationId == applicationId
                        && r.ClientIp == clientIp
                        && r.Status == AccessRequestStatus.Pending
                        && r.ExpiresAt > DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query = query.Where(r => r.SessionId == sessionId);
        }
        else
        {
            query = query.Where(r => r.SessionId == null);
        }

        if (!string.IsNullOrWhiteSpace(deviceFingerprint))
        {
            query = query.Where(r => r.DeviceFingerprint == deviceFingerprint);
        }
        else
        {
            query = query.Where(r => r.DeviceFingerprint == null);
        }

        return query
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.AccessRequests
            .AsNoTracking()
            .Include(r => r.Application)
            .Include(r => r.IpAddress)
            .Include(r => r.User)
            .Where(r => r.Status == AccessRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return requests.AsReadOnly();
    }

    public async Task<IReadOnlyList<AccessRequest>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var requests = await _context.AccessRequests
            .AsNoTracking()
            .Include(r => r.Application)
            .Include(r => r.IpAddress)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return requests.AsReadOnly();
    }

    public async Task<IReadOnlyList<AccessRequest>> GetByIpAsync(string ip, int limit, CancellationToken cancellationToken = default)
    {
        var requests = await _context.AccessRequests
            .AsNoTracking()
            .Where(r => r.ClientIp == ip)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return requests.AsReadOnly();
    }

    public async Task<IReadOnlyList<AccessRequest>> GetByDeviceFingerprintAsync(string fingerprint, int limit, CancellationToken cancellationToken = default)
    {
        var requests = await _context.AccessRequests
            .AsNoTracking()
            .Where(r => r.DeviceFingerprint == fingerprint)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return requests.AsReadOnly();
    }

    public async Task AddAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        await _context.AccessRequests.AddAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        _context.AccessRequests.Update(request);
        return Task.CompletedTask;
    }
}
