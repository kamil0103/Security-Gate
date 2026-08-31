using SecurityGateway.Domain.Applications;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Domain.AccessControl;

public enum TrustScope
{
    Session,
    Device,
    IpAndDevice,
    Ip,
    Permanent
}

public sealed class TrustRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public TrustScope Scope { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }

    public Guid ApplicationId { get; init; }
    public Application Application { get; init; } = null!;

    public string ClientIp { get; init; } = string.Empty;
    public string? DeviceFingerprint { get; init; }
    public Guid? UserId { get; init; }
    public User? User { get; init; }
    public string? SessionId { get; init; }

    public Guid? AccessRequestId { get; init; }
    public AccessRequest AccessRequest { get; init; } = null!;

    public Guid? CreatedByUserId { get; init; }
}
