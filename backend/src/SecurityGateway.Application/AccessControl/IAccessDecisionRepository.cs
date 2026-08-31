using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl;

public interface IAccessDecisionRepository
{
    Task<AccessDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessDecision>> GetByTargetAsync(AccessDecisionType type, Guid targetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessDecision>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task AddAsync(AccessDecision decision, CancellationToken cancellationToken = default);
}
