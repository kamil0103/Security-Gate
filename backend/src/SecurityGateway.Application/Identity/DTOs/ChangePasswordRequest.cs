namespace SecurityGateway.Application.Identity.DTOs;

public sealed record ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}
