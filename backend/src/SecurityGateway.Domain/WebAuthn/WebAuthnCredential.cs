namespace SecurityGateway.Domain.WebAuthn;

public sealed class WebAuthnCredential
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required string CredentialId { get; init; }
    public required string PublicKey { get; init; }
    public required string CredentialType { get; init; }
    public required string Transports { get; init; }
    public long SignatureCounter { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
}
