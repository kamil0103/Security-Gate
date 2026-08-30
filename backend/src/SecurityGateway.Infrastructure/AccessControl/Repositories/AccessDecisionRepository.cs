using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.AccessControl.Repositories;

public sealed class AccessDecisionRepository : IAccessDecisionRepository
{
    private readonly ApplicationDbContext _context;

    public AccessDecisionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AccessDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.AccessDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AccessDecision>> GetByTargetAsync(AccessDecisionType type, Guid targetId, CancellationToken cancellationToken = default)
    {
        var decisions = await _context.AccessDecisions
            .AsNoTracking()
            .Where(d => d.Type == type && d.TargetId == targetId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return decisions.AsReadOnly();
    }

    public async Task<IReadOnlyList<AccessDecision>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var decisions = await _context.AccessDecisions
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return decisions.AsReadOnly();
    }

    public async Task AddAsync(AccessDecision decision, CancellationToken cancellationToken = default)
    {
        await _context.AccessDecisions.AddAsync(decision, cancellationToken).ConfigureAwait(false);
    }
}
