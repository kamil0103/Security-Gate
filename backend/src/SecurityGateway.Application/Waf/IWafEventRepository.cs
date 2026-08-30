using SecurityGateway.Domain.Waf;

namespace SecurityGateway.Application.Waf;

public interface IWafEventRepository
{
    Task<WafEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(WafEvent wafEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WafEvent>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WafEvent>> SearchAsync(
        string? sourceIp = null,
        AttackType? attackType = null,
        AttackSeverity? severity = null,
        WafAction? action = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}
