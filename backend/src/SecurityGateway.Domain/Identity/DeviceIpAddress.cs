namespace SecurityGateway.Domain.Identity;

public sealed class DeviceIpAddress
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DeviceId { get; init; }
    public Device Device { get; init; } = null!;

    public required string IpAddress { get; init; }
    public DateTimeOffset FirstSeenAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public int RequestCount { get; set; } = 1;
}
