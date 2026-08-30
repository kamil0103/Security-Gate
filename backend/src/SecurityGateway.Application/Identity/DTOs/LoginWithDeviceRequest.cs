namespace SecurityGateway.Application.Identity.DTOs;

public sealed record LoginWithDeviceRequest
{
    public required LoginRequest User { get; init; }
    public DeviceEnrollmentRequest? Device { get; init; }
}
