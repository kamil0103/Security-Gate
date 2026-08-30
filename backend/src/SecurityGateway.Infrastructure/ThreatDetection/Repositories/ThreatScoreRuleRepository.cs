using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.ThreatDetection.Repositories;

public sealed class ThreatScoreRuleRepository : IThreatScoreRuleRepository
{
    private readonly ApplicationDbContext _context;

    public ThreatScoreRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ThreatScoreRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ThreatScoreRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ThreatScoreRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _context.ThreatScoreRules
            .AsNoTracking()
            .OrderBy(r => r.EventType)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rules.AsReadOnly();
    }

    public async Task<IReadOnlyList<ThreatScoreRule>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _context.ThreatScoreRules
            .AsNoTracking()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.EventType)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rules.AsReadOnly();
    }

    public async Task AddAsync(ThreatScoreRule rule, CancellationToken cancellationToken = default)
    {
        await _context.ThreatScoreRules.AddAsync(rule, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ThreatScoreRule rule, CancellationToken cancellationToken = default)
    {
        var tracked = _context.ThreatScoreRules.Local.FirstOrDefault(r => r.Id == rule.Id);

        if (tracked is not null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(rule);
        }
        else
        {
            _context.ThreatScoreRules.Update(rule);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(ThreatScoreRule rule, CancellationToken cancellationToken = default)
    {
        _context.ThreatScoreRules.Remove(rule);
        return Task.CompletedTask;
    }
}
