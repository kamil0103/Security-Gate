using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Domain.RateLimiting;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.RateLimiting.Repositories;

public sealed class RateLimitRuleRepository : IRateLimitRuleRepository
{
    private readonly ApplicationDbContext _context;

    public RateLimitRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<RateLimitRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.RateLimitRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RateLimitRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _context.RateLimitRules
            .AsNoTracking()
            .OrderBy(r => r.ScopeType)
            .ThenBy(r => r.ScopeValue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rules.AsReadOnly();
    }

    public async Task<IReadOnlyList<RateLimitRule>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _context.RateLimitRules
            .AsNoTracking()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.ScopeType)
            .ThenBy(r => r.ScopeValue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rules.AsReadOnly();
    }

    public async Task AddAsync(RateLimitRule rule, CancellationToken cancellationToken = default)
    {
        await _context.RateLimitRules.AddAsync(rule, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(RateLimitRule rule, CancellationToken cancellationToken = default)
    {
        var tracked = _context.RateLimitRules.Local.FirstOrDefault(r => r.Id == rule.Id);

        if (tracked is not null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(rule);
        }
        else
        {
            _context.RateLimitRules.Update(rule);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(RateLimitRule rule, CancellationToken cancellationToken = default)
    {
        _context.RateLimitRules.Remove(rule);
        return Task.CompletedTask;
    }
}
