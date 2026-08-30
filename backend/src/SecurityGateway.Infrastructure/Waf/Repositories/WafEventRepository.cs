using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Waf;
using SecurityGateway.Domain.Waf;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Waf.Repositories;

public sealed class WafEventRepository : IWafEventRepository
{
    private readonly ApplicationDbContext _context;

    public WafEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<WafEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.WafEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(WafEvent wafEvent, CancellationToken cancellationToken = default)
    {
        await _context.WafEvents.AddAsync(wafEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WafEvent>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var events = await _context.WafEvents
            .AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return events.AsReadOnly();
    }

    public async Task<IReadOnlyList<WafEvent>> SearchAsync(
        string? sourceIp = null,
        AttackType? attackType = null,
        AttackSeverity? severity = null,
        WafAction? action = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WafEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(sourceIp))
        {
            query = query.Where(e => e.SourceIp == sourceIp);
        }

        if (attackType.HasValue)
        {
            query = query.Where(e => e.AttackType == attackType.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(e => e.Severity == severity.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(e => e.Action == action.Value);
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
}
