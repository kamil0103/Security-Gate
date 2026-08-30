using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity.DTOs;

public sealed record DeviceRecognitionResult
{
    public required DeviceDto? Device { get; init; }
    public required bool IsKnown { get; init; }
    public required bool IsTrusted { get; init; }
    public required DeviceTrustStatus TrustStatus { get; init; }
}
