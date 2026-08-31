namespace SecurityGateway.Application.Blocking.DTOs;

public sealed record BlockResultDto
{
    public required bool Blocked { get; init; }
    public required string IpAddress { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Reason { get; init; }
}
