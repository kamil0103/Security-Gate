using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity.DTOs;

public sealed record DeviceDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string Fingerprint { get; init; }
    public string? UserAgent { get; init; }
    public string? OperatingSystem { get; init; }
    public string? Browser { get; init; }
    public required DeviceTrustStatus TrustStatus { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastSeenAt { get; init; }
}
