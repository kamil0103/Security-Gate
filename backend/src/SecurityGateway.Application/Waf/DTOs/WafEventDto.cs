using SecurityGateway.Domain.Waf;

namespace SecurityGateway.Application.Waf.DTOs;

public sealed record WafEventDto
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string SourceIp { get; init; }
    public string? RequestId { get; init; }
    public required string RuleId { get; init; }
    public string? RuleMessage { get; init; }
    public AttackSeverity Severity { get; init; }
    public AttackType AttackType { get; init; }
    public required string Method { get; init; }
    public required string Uri { get; init; }
    public string? Host { get; init; }
    public WafAction Action { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
}
