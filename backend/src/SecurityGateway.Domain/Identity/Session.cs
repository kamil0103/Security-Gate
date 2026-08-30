namespace SecurityGateway.Domain.Identity;

public sealed class Session
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
    public required string RefreshTokenHash { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
}
