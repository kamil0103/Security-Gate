namespace SecurityGateway.Application.AccessControl.Models;

public sealed record DeviceTrustResult
{
    public required bool IsTrusted { get; init; }
    public required bool IsPending { get; init; }
    public required bool IsBlocked { get; init; }
    public string? Reason { get; init; }
}
