namespace SecurityGateway.Application.Identity.DTOs;

public sealed record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
