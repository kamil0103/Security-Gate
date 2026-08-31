using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.ThreatDetection.Repositories;

public sealed class SecurityEventRepository : ISecurityEventRepository
{
    private readonly ApplicationDbContext _context;

    public SecurityEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<SecurityEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.SecurityEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        await _context.SecurityEvents.AddAsync(securityEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SecurityEvent>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var events = await _context.SecurityEvents
            .AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return events.AsReadOnly();
    }

    public async Task<IReadOnlyList<SecurityEvent>> SearchAsync(
        SecurityEventType? type = null,
        SecurityEventSeverity? severity = null,
        string? sourceIp = null,
        Guid? userId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityEvents.AsNoTracking().AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(e => e.Type == type.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(e => e.Severity == severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceIp))
        {
            query = query.Where(e => e.SourceIp == sourceIp);
        }

        if (userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.Timestamp <= to.Value);
        }

        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return events.AsReadOnly();
    }

    public async Task<int> CountEventsAsync(string sourceIp, SecurityEventType type, DateTimeOffset from, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityEvents
            .AsNoTracking()
            .Where(e => e.SourceIp == sourceIp && e.Type == type && e.Timestamp >= from)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
