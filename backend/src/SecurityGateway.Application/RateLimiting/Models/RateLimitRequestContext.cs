namespace SecurityGateway.Application.RateLimiting.Models;

public sealed record RateLimitRequestContext
{
    public required string IpAddress { get; init; }
    public Guid? UserId { get; init; }
    public Guid? DeviceId { get; init; }
    public required string Domain { get; init; }
    public required string Endpoint { get; init; }
}
