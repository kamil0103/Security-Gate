using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SecurityGateway.Tests.Identity;

public class DeviceIdentityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DeviceIdentityService _service;
    private readonly Guid _userId;

    public DeviceIdentityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var deviceRepository = new DeviceRepository(_context);
        _service = new DeviceIdentityService(deviceRepository, _context);

        var user = new User
        {
            Username = "devicetest",
            Email = "device@example.com",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            EmailVerified = true
        };

        _context.Users.Add(user);
        _context.SaveChangesAsync().GetAwaiter().GetResult();
        _userId = user.Id;
    }

    [Fact]
    public async Task RecognizeOrEnrollAsync_FirstDevice_IsTrusted()
    {
        var request = CreateRequest("device-1", "fingerprint-1");

        var result = await _service.RecognizeOrEnrollAsync(_userId, request, "192.168.1.1");

        Assert.True(result.IsKnown);
        Assert.True(result.IsTrusted);
        Assert.Equal(DeviceTrustStatus.Trusted, result.TrustStatus);
    }

    [Fact]
    public async Task RecognizeOrEnrollAsync_SecondDevice_IsPending()
    {
        await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-1", "fingerprint-1"), "192.168.1.1");

        var result = await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-2", "fingerprint-2"), "192.168.1.2");

        Assert.True(result.IsKnown);
        Assert.False(result.IsTrusted);
        Assert.Equal(DeviceTrustStatus.Pending, result.TrustStatus);
    }

    [Fact]
    public async Task RecognizeOrEnrollAsync_KnownFingerprint_ReturnsExistingDevice()
    {
        var first = await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-1", "fingerprint-1"), "192.168.1.1");

        var second = await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-1", "fingerprint-1"), "192.168.1.2");

        Assert.Equal(first.Device!.Id, second.Device!.Id);
    }

    [Fact]
    public async Task RecognizeOrEnrollAsync_ExistingDevice_DoesNotWriteActivityOrOverwriteTrust()
    {
        var request = CreateRequest("device-1", "fingerprint-1");
        var enrolled = await _service.RecognizeOrEnrollAsync(_userId, request, "192.168.1.1");
        var device = await _context.Devices.SingleAsync(d => d.Id == enrolled.Device!.Id);
        var originalLastSeen = device.LastSeenAt;
        var originalIpCount = device.IpHistory.Sum(ip => ip.RequestCount);

        await _service.BlockDeviceAsync(_userId, device.Id);
        _context.ChangeTracker.Clear();

        var recognized = await _service.RecognizeOrEnrollAsync(
            _userId, CreateRequest("device-1", "fingerprint-1"), "192.168.1.2");

        Assert.Equal(DeviceTrustStatus.Blocked, recognized.TrustStatus);
        Assert.False(recognized.IsTrusted);
        var persisted = await _context.Devices.SingleAsync(d => d.Id == device.Id);
        Assert.Equal(originalLastSeen, persisted.LastSeenAt);
        Assert.Equal(originalIpCount, persisted.IpHistory.Sum(ip => ip.RequestCount));
        Assert.DoesNotContain(persisted.IpHistory, ip => ip.IpAddress == "192.168.1.2");
        Assert.False(_context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task TrustDeviceAsync_PendingDevice_BecomesTrusted()
    {
        await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-1", "fingerprint-1"), "192.168.1.1");
        var pending = await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-2", "fingerprint-2"), "192.168.1.2");

        await _service.TrustDeviceAsync(_userId, pending.Device!.Id);
        var device = await _service.GetByIdAsync(_userId, pending.Device.Id);

        Assert.Equal(DeviceTrustStatus.Trusted, device!.TrustStatus);
    }

    [Fact]
    public async Task BlockDeviceAsync_Device_BecomesBlocked()
    {
        var result = await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-1", "fingerprint-1"), "192.168.1.1");

        await _service.BlockDeviceAsync(_userId, result.Device!.Id);
        var device = await _service.GetByIdAsync(_userId, result.Device.Id);

        Assert.Equal(DeviceTrustStatus.Blocked, device!.TrustStatus);
    }

    [Fact]
    public async Task GetPendingDevicesAsync_ReturnsOnlyPendingDevices()
    {
        await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-1", "fingerprint-1"), "192.168.1.1");
        await _service.RecognizeOrEnrollAsync(_userId, CreateRequest("device-2", "fingerprint-2"), "192.168.1.2");

        var pending = await _service.GetPendingDevicesAsync(_userId);

        Assert.Single(pending);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static DeviceEnrollmentRequest CreateRequest(string deviceId, string fingerprint)
    {
        return new DeviceEnrollmentRequest
        {
            DeviceId = deviceId,
            Name = "Test Device",
            Fingerprint = fingerprint,
            OperatingSystem = "Linux",
            Browser = "Firefox"
        };
    }
}
