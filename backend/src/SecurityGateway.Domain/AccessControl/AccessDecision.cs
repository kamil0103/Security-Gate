using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Domain.AccessControl;

public sealed class AccessDecision
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required AccessDecisionType Type { get; init; }
    public required Guid TargetId { get; init; }
    public required AccessDecisionOutcome Outcome { get; init; }
    public string? Reason { get; set; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
