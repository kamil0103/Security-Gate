using SecurityGateway.Domain.RateLimiting;

namespace SecurityGateway.Application.RateLimiting;

public interface IRateLimitRuleRepository
{
    Task<RateLimitRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RateLimitRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RateLimitRule>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RateLimitRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(RateLimitRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(RateLimitRule rule, CancellationToken cancellationToken = default);
}
