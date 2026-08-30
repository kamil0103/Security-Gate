namespace SecurityGateway.Application.Identity.DTOs;

public sealed record RegisterRequest
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
}
