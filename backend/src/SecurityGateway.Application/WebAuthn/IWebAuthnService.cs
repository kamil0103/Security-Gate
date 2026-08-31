namespace SecurityGateway.Application.WebAuthn;

public interface IWebAuthnService
{
    Task<WebAuthnChallengeDto> BeginRegistrationAsync(Guid userId, string username, CancellationToken cancellationToken = default);
    Task CompleteRegistrationAsync(Guid userId, CompleteRegistrationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebAuthnCredentialDto>> GetCredentialsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteCredentialAsync(Guid userId, string credentialId, CancellationToken cancellationToken = default);
}

public class WebAuthnChallengeDto
{
    public required string Challenge { get; set; }
    public required string RelyingPartyId { get; set; }
    public required string RelyingPartyName { get; set; }
    public required Guid UserId { get; set; }
    public required string Username { get; set; }
}

public class CompleteRegistrationRequest
{
    public required string CredentialId { get; set; }
    public required string PublicKey { get; set; }
    public required string CredentialType { get; set; }
    public string? Transports { get; set; }
    public string? DeviceName { get; set; }
}

public class WebAuthnCredentialDto
{
    public Guid Id { get; set; }
    public required string CredentialId { get; set; }
    public required string CredentialType { get; set; }
    public string? DeviceName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastUsedAt { get; set; }
    public bool IsEnabled { get; set; }
}
