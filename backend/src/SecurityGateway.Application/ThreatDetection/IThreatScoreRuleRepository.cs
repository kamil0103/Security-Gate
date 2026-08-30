using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Application.ThreatDetection;

public interface IThreatScoreRuleRepository
{
    Task<ThreatScoreRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ThreatScoreRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ThreatScoreRule>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ThreatScoreRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(ThreatScoreRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(ThreatScoreRule rule, CancellationToken cancellationToken = default);
}
