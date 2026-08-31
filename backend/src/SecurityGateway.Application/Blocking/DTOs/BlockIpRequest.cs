namespace SecurityGateway.Application.Blocking.DTOs;

public sealed record BlockIpRequest
{
    public required string IpAddress { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Reason { get; init; }
}
