using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity;

public interface ITokenRepository
{
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task AddEmailVerificationTokenAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    Task UpdatePasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task UpdateEmailVerificationTokenAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
}
