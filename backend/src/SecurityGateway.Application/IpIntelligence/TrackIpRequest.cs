namespace SecurityGateway.Application.IpIntelligence;

public sealed record TrackIpRequest
{
    public required string IpAddress { get; init; }
    public Guid? UserId { get; init; }
    public Guid? DeviceId { get; init; }
}
