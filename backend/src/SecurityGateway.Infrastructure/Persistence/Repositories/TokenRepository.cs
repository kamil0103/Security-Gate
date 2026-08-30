using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Infrastructure.Persistence.Repositories;

public sealed class TokenRepository : ITokenRepository
{
    private readonly ApplicationDbContext _context;

    public TokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<PasswordResetToken?> GetPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _context.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddEmailVerificationTokenAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdatePasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Update(token);
        return Task.CompletedTask;
    }

    public Task UpdateEmailVerificationTokenAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        _context.EmailVerificationTokens.Update(token);
        return Task.CompletedTask;
    }
}
