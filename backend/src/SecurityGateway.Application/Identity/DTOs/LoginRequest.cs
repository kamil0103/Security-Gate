namespace SecurityGateway.Application.Identity.DTOs;

public sealed record LoginRequest
{
    public required string UsernameOrEmail { get; init; }
    public required string Password { get; init; }
}
