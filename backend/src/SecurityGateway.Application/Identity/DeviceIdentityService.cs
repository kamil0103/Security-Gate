using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity;

public sealed class DeviceIdentityService : IDeviceIdentityService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeviceIdentityService(IDeviceRepository deviceRepository, IUnitOfWork unitOfWork)
    {
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeviceRecognitionResult> RecognizeOrEnrollAsync(
        Guid userId,
        DeviceEnrollmentRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var existingDevice = await _deviceRepository.GetByUserAndFingerprintAsync(userId, request.Fingerprint, cancellationToken).ConfigureAwait(false)
            ?? await _deviceRepository.GetByUserAndDeviceIdAsync(userId, request.DeviceId, cancellationToken).ConfigureAwait(false);

        if (existingDevice is not null)
        {
            UpdateDeviceSignals(existingDevice, request);
            UpdateIpHistory(existingDevice, ipAddress);
            existingDevice.LastSeenAt = DateTimeOffset.UtcNow;

            await _deviceRepository.UpdateAsync(existingDevice, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return MapToRecognitionResult(existingDevice);
        }

        // A user's first device is automatically trusted for convenience.
        // Subsequent devices start as pending approval.
        var userHasDevices = await _deviceRepository.ExistsForUserAsync(userId, request.Fingerprint, cancellationToken).ConfigureAwait(false);
        var initialTrustStatus = userHasDevices ? DeviceTrustStatus.Pending : DeviceTrustStatus.Trusted;

        var device = new Device
        {
            UserId = userId,
            Name = request.Name,
            Fingerprint = request.Fingerprint,
            PublicKey = request.PublicKey,
            UserAgent = request.UserAgent,
            OperatingSystem = request.OperatingSystem,
            Browser = request.Browser,
            TrustStatus = initialTrustStatus
        };

        // Store the client-provided device ID in CredentialId for correlation.
        // The actual database ID is a separate Guid.
        device.CredentialId = request.DeviceId;

        UpdateIpHistory(device, ipAddress);

        await _deviceRepository.AddAsync(device, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToRecognitionResult(device);
    }

    public async Task<DeviceDto?> GetByIdAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false);
        return device?.UserId == userId ? MapToDto(device) : null;
    }

    public async Task<IReadOnlyList<DeviceDto>> GetUserDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await _deviceRepository.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return devices.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<DeviceDto>> GetPendingDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await _deviceRepository.GetPendingByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return devices.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task TrustDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        await UpdateTrustStatusAsync(userId, deviceId, DeviceTrustStatus.Trusted, cancellationToken).ConfigureAwait(false);
    }

    public async Task UntrustDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        await UpdateTrustStatusAsync(userId, deviceId, DeviceTrustStatus.Untrusted, cancellationToken).ConfigureAwait(false);
    }

    public async Task BlockDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        await UpdateTrustStatusAsync(userId, deviceId, DeviceTrustStatus.Blocked, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false);

        if (device is null || device.UserId != userId)
        {
            throw new AuthenticationException("Device not found.");
        }

        // Prevent removing the last trusted device to avoid lockout.
        var userDevices = await _deviceRepository.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var trustedCount = userDevices.Count(d => d.TrustStatus == DeviceTrustStatus.Trusted && d.Id != deviceId);

        if (trustedCount == 0)
        {
            throw new AuthenticationException("Cannot remove the last trusted device.");
        }

        device.TrustStatus = DeviceTrustStatus.Blocked;
        await _deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateTrustStatusAsync(Guid userId, Guid deviceId, DeviceTrustStatus status, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false);

        if (device is null || device.UserId != userId)
        {
            throw new AuthenticationException("Device not found.");
        }

        device.TrustStatus = status;
        await _deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void UpdateDeviceSignals(Device device, DeviceEnrollmentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            device.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.UserAgent))
        {
            device.UserAgent = request.UserAgent;
        }

        if (!string.IsNullOrWhiteSpace(request.OperatingSystem))
        {
            device.OperatingSystem = request.OperatingSystem;
        }

        if (!string.IsNullOrWhiteSpace(request.Browser))
        {
            device.Browser = request.Browser;
        }

        if (!string.IsNullOrWhiteSpace(request.PublicKey))
        {
            device.PublicKey = request.PublicKey;
        }
    }

    private static void UpdateIpHistory(Device device, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return;
        }

        var existingIp = device.IpHistory.FirstOrDefault(ip => ip.IpAddress == ipAddress);

        if (existingIp is not null)
        {
            existingIp.LastSeenAt = DateTimeOffset.UtcNow;
            existingIp.RequestCount++;
        }
        else
        {
            device.IpHistory.Add(new DeviceIpAddress
            {
                IpAddress = ipAddress
            });
        }
    }

    private static DeviceRecognitionResult MapToRecognitionResult(Device device)
    {
        return new DeviceRecognitionResult
        {
            Device = MapToDto(device),
            IsKnown = true,
            IsTrusted = device.TrustStatus == DeviceTrustStatus.Trusted,
            TrustStatus = device.TrustStatus
        };
    }

    private static DeviceDto MapToDto(Device device)
    {
        return new DeviceDto
        {
            Id = device.Id,
            UserId = device.UserId,
            Name = device.Name,
            Fingerprint = device.Fingerprint,
            UserAgent = device.UserAgent,
            OperatingSystem = device.OperatingSystem,
            Browser = device.Browser,
            TrustStatus = device.TrustStatus,
            CreatedAt = device.CreatedAt,
            LastSeenAt = device.LastSeenAt
        };
    }
}
