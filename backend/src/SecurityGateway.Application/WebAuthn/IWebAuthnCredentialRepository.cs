using SecurityGateway.Domain.WebAuthn;

namespace SecurityGateway.Application.WebAuthn;

public interface IWebAuthnCredentialRepository
{
    Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WebAuthnCredential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default);
    Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default);
    void Update(WebAuthnCredential credential);
    void Delete(WebAuthnCredential credential);
}
