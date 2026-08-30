using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.AccessControl.Services;
using SecurityGateway.Infrastructure.Identity;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using SecurityGateway.Tests.TestHelpers;
using Xunit;

namespace SecurityGateway.Tests.AccessControl;

public class AccessControlServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AccessControlService _service;

    public AccessControlServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var trustedNetworkRepository = new TrustedNetworkRepository(_context);
        var blocklistRepository = new BlocklistRepository(_context);
        var accessDecisionRepository = new AccessDecisionRepository(_context);
        var deviceRepository = new DeviceRepository(_context);

        _service = new AccessControlService(
            trustedNetworkRepository,
            blocklistRepository,
            accessDecisionRepository,
            deviceRepository,
            new FakeThreatDetectionService(),
            _context);
    }

    [Fact]
    public async Task IsIpTrustedAsync_MatchingCidr_ReturnsTrue()
    {
        await _service.CreateTrustedNetworkAsync(new CreateTrustedNetworkRequest
        {
            Name = "Home LAN",
            Cidr = "192.168.1.0/24"
        });

        var result = await _service.IsIpTrustedAsync("192.168.1.50");

        Assert.True(result);
    }

    [Fact]
    public async Task IsIpTrustedAsync_NonMatchingCidr_ReturnsFalse()
    {
        await _service.CreateTrustedNetworkAsync(new CreateTrustedNetworkRequest
        {
            Name = "Home LAN",
            Cidr = "192.168.1.0/24"
        });

        var result = await _service.IsIpTrustedAsync("10.0.0.5");

        Assert.False(result);
    }

    [Fact]
    public async Task IsBlockedAsync_BlockedIp_ReturnsTrue()
    {
        await _service.CreateBlocklistEntryAsync(new CreateBlocklistEntryRequest
        {
            Type = BlocklistEntryType.Ip,
            Value = "198.51.100.5"
        });

        var result = await _service.IsBlockedAsync("198.51.100.5", null, null);

        Assert.True(result);
    }

    [Fact]
    public async Task IsBlockedAsync_BlockedNetwork_ReturnsTrue()
    {
        await _service.CreateBlocklistEntryAsync(new CreateBlocklistEntryRequest
        {
            Type = BlocklistEntryType.Network,
            Value = "198.51.100.0/24"
        });

        var result = await _service.IsBlockedAsync("198.51.100.50", null, null);

        Assert.True(result);
    }

    [Fact]
    public async Task IsBlockedAsync_ExpiredEntry_ReturnsFalse()
    {
        await _service.CreateBlocklistEntryAsync(new CreateBlocklistEntryRequest
        {
            Type = BlocklistEntryType.Ip,
            Value = "198.51.100.5",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        var result = await _service.IsBlockedAsync("198.51.100.5", null, null);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateDeviceTrustAsync_TrustedNetworkAutoApprovesPendingDevice()
    {
        var user = await CreateUserAsync();
        var device = await CreateDeviceAsync(user.Id, DeviceTrustStatus.Pending);

        await _service.CreateTrustedNetworkAsync(new CreateTrustedNetworkRequest
        {
            Name = "Trusted",
            Cidr = "10.0.0.0/8"
        });

        var result = await _service.EvaluateDeviceTrustAsync(user.Id, device.Id, "10.0.0.5");

        Assert.True(result.IsTrusted);
        Assert.False(result.IsPending);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public async Task EvaluateDeviceTrustAsync_BlockedDevice_ReturnsBlocked()
    {
        var user = await CreateUserAsync();
        var device = await CreateDeviceAsync(user.Id, DeviceTrustStatus.Blocked);

        var result = await _service.EvaluateDeviceTrustAsync(user.Id, device.Id, "10.0.0.5");

        Assert.True(result.IsBlocked);
        Assert.False(result.IsTrusted);
    }

    [Fact]
    public async Task ApproveDeviceAsync_SetsDeviceTrustedAndCreatesDecision()
    {
        var user = await CreateUserAsync();
        var device = await CreateDeviceAsync(user.Id, DeviceTrustStatus.Pending);

        var decision = await _service.ApproveDeviceAsync(device.Id, user.Id, "admin approved");

        Assert.Equal(AccessDecisionOutcome.Approved, decision.Outcome);

        var decisions = await _service.GetDecisionsForTargetAsync(AccessDecisionType.DeviceApproval, device.Id);
        Assert.Single(decisions);
    }

    [Fact]
    public async Task DenyDeviceAsync_SetsDeviceUntrustedAndCreatesDecision()
    {
        var user = await CreateUserAsync();
        var device = await CreateDeviceAsync(user.Id, DeviceTrustStatus.Pending);

        var decision = await _service.DenyDeviceAsync(device.Id, user.Id, "suspicious");

        Assert.Equal(AccessDecisionOutcome.Denied, decision.Outcome);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<User> CreateUserAsync()
    {
        var user = new User
        {
            Username = $"user{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            EmailVerified = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Device> CreateDeviceAsync(Guid userId, DeviceTrustStatus status)
    {
        var device = new Device
        {
            UserId = userId,
            Name = "Test Device",
            Fingerprint = Guid.NewGuid().ToString(),
            TrustStatus = status
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        return device;
    }
}
