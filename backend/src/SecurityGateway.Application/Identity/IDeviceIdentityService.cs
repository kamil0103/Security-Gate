using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity;

public interface IDeviceIdentityService
{
    Task<DeviceRecognitionResult> RecognizeOrEnrollAsync(
        Guid userId,
        DeviceEnrollmentRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default);

    Task<DeviceDto?> GetByIdAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceDto>> GetUserDevicesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceDto>> GetPendingDevicesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task TrustDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
    Task UntrustDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
    Task BlockDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
    Task RemoveDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
}
