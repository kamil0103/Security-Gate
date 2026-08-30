namespace SecurityGateway.Application.Identity.DTOs;

public sealed record ForgotPasswordRequest
{
    public required string Email { get; init; }
}
