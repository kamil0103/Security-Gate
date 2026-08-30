namespace SecurityGateway.Domain.Identity;

public sealed class EmailVerificationToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
    public required string TokenHash { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UsedAt { get; set; }
    public bool IsUsed => UsedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
}
