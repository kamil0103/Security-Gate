namespace SecurityGateway.Application.Applications.Models;

public sealed record ApplicationPolicyEvaluation
{
    public required bool Allowed { get; init; }
    public required string? Reason { get; init; }
    public required bool RequiresAuthentication { get; init; }
    public required bool IsAuthenticated { get; init; }
}
