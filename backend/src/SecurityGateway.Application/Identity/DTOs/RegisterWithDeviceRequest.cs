namespace SecurityGateway.Application.Identity.DTOs;

public sealed record RegisterWithDeviceRequest
{
    public required RegisterRequest User { get; init; }
    public DeviceEnrollmentRequest? Device { get; init; }
}
