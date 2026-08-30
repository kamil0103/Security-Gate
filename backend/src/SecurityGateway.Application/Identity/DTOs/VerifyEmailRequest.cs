namespace SecurityGateway.Application.Identity.DTOs;

public sealed record VerifyEmailRequest
{
    public required string Email { get; init; }
    public required string Token { get; init; }
}
