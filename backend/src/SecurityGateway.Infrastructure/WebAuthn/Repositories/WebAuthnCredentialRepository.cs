using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.WebAuthn;
using SecurityGateway.Domain.WebAuthn;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.WebAuthn.Repositories;

public class WebAuthnCredentialRepository : IWebAuthnCredentialRepository
{
    private readonly ApplicationDbContext _context;

    public WebAuthnCredentialRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.WebAuthnCredentials
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<WebAuthnCredential>)t.Result, cancellationToken);

    public Task<WebAuthnCredential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default)
        => _context.WebAuthnCredentials.FirstOrDefaultAsync(c => c.CredentialId == credentialId, cancellationToken);

    public Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
        => _context.WebAuthnCredentials.AddAsync(credential, cancellationToken).AsTask();

    public void Update(WebAuthnCredential credential)
    {
        _context.WebAuthnCredentials.Update(credential);
    }

    public void Delete(WebAuthnCredential credential)
    {
        _context.WebAuthnCredentials.Remove(credential);
    }
}
