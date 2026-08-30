using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity.DTOs;

public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public required UserStatus Status { get; init; }
    public required bool EmailVerified { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? LastLoginAt { get; init; }
}
