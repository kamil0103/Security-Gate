using SecurityGateway.Application.RateLimiting.DTOs;
using SecurityGateway.Application.RateLimiting.Models;

namespace SecurityGateway.Application.RateLimiting;

public interface IRateLimitService
{
    Task<RateLimitResult> CheckAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RateLimitRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task<RateLimitRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RateLimitRuleDto> CreateRuleAsync(CreateRateLimitRuleRequest request, CancellationToken cancellationToken = default);
    Task<RateLimitRuleDto> UpdateRuleAsync(Guid id, CreateRateLimitRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);
}
