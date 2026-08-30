namespace SecurityGateway.Application.Identity.DTOs;

public sealed record LoginResponse
{
    public required UserDto User { get; init; }
    public required TokenPair Tokens { get; init; }
    public required DeviceRecognitionResult Device { get; init; }
}
