using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Domain.IpIntelligence;

public sealed class IpDeviceAssociation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid IpAddressId { get; init; }
    public IpAddress IpAddress { get; init; } = null!;
    public Guid DeviceId { get; init; }
    public Device Device { get; init; } = null!;
    public DateTimeOffset FirstSeenAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public long RequestCount { get; set; } = 1;
}
