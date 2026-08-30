namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed record AccessDecisionRequest
{
    public string? Reason { get; init; }
}
