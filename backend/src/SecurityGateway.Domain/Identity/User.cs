namespace SecurityGateway.Domain.Identity;

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool EmailVerified { get; set; }

    public ICollection<Session> Sessions { get; init; } = new List<Session>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; init; } = new List<PasswordResetToken>();
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; init; } = new List<EmailVerificationToken>();
}
