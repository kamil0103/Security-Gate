using System.Security.Cryptography;
using SecurityGateway.Application.WebAuthn;
using SecurityGateway.Domain.WebAuthn;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.WebAuthn.Services;

public class WebAuthnService : IWebAuthnService
{
    private readonly IWebAuthnCredentialRepository _credentialRepository;
    private readonly WebAuthnOptions _options;
    private readonly ApplicationDbContext _context;

    public WebAuthnService(
        IWebAuthnCredentialRepository credentialRepository,
        WebAuthnOptions options,
        ApplicationDbContext context)
    {
        _credentialRepository = credentialRepository;
        _options = options;
        _context = context;
    }

    public Task<WebAuthnChallengeDto> BeginRegistrationAsync(Guid userId, string username, CancellationToken cancellationToken = default)
    {
        var challenge = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        return Task.FromResult(new WebAuthnChallengeDto
        {
            Challenge = challenge,
            RelyingPartyId = _options.RelyingPartyId,
            RelyingPartyName = _options.RelyingPartyName,
            UserId = userId,
            Username = username
        });
    }

    public async Task CompleteRegistrationAsync(Guid userId, CompleteRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var credential = new WebAuthnCredential
        {
            UserId = userId,
            CredentialId = request.CredentialId,
            PublicKey = request.PublicKey,
            CredentialType = request.CredentialType,
            Transports = request.Transports ?? "internal",
            DeviceName = request.DeviceName
        };

        await _credentialRepository.AddAsync(credential, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WebAuthnCredentialDto>> GetCredentialsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        return credentials.Select(c => new WebAuthnCredentialDto
        {
            Id = c.Id,
            CredentialId = c.CredentialId,
            CredentialType = c.CredentialType,
            DeviceName = c.DeviceName,
            CreatedAt = c.CreatedAt,
            LastUsedAt = c.LastUsedAt,
            IsEnabled = c.IsEnabled
        }).ToList();
    }

    public async Task DeleteCredentialAsync(Guid userId, string credentialId, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByCredentialIdAsync(credentialId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credential not found.");

        if (credential.UserId != userId)
        {
            throw new InvalidOperationException("Credential does not belong to the user.");
        }

        _credentialRepository.Delete(credential);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
