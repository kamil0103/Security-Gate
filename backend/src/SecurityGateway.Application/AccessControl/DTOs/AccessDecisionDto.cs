using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed record AccessDecisionDto
{
    public required Guid Id { get; init; }
    public required AccessDecisionType Type { get; init; }
    public required Guid TargetId { get; init; }
    public required AccessDecisionOutcome Outcome { get; init; }
    public string? Reason { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
