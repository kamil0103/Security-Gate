namespace SecurityGateway.Application.Identity.DTOs;

public sealed record DeviceEnrollmentRequest
{
    public required string DeviceId { get; init; }
    public required string Name { get; init; }
    public required string Fingerprint { get; init; }
    public string? PublicKey { get; init; }
    public string? UserAgent { get; init; }
    public string? OperatingSystem { get; init; }
    public string? Browser { get; init; }
}
