namespace SecurityGateway.Domain.Identity;

public sealed class Device
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;

    public required string Name { get; set; }
    public required string Fingerprint { get; set; }
    public string? PublicKey { get; set; }
    public string? CredentialId { get; set; }

    public string? UserAgent { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Browser { get; set; }

    public DeviceTrustStatus TrustStatus { get; set; } = DeviceTrustStatus.Pending;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<DeviceIpAddress> IpHistory { get; init; } = new List<DeviceIpAddress>();
}
