using SecurityGateway.Domain.Waf;

namespace SecurityGateway.Application.Waf.DTOs;

public sealed record WafEventFilter
{
    public string? SourceIp { get; init; }
    public AttackType? AttackType { get; init; }
    public AttackSeverity? Severity { get; init; }
    public WafAction? Action { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
