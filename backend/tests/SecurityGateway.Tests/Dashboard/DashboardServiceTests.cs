using Microsoft.EntityFrameworkCore;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.IpIntelligence;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Domain.Waf;
using SecurityGateway.Infrastructure.Dashboard.Services;
using SecurityGateway.Infrastructure.Persistence;
using ApplicationEntity = SecurityGateway.Domain.Applications.Application;
using Xunit;

namespace SecurityGateway.Tests.Dashboard;

public class DashboardServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new DashboardService(_context);
    }

    [Fact]
    public async Task GetOverviewAsync_ReturnsCounts()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = userId,
            Username = "u1",
            Email = "u1@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            EmailVerified = true
        });

        _context.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Device 1",
            Fingerprint = "fp1",
            TrustStatus = DeviceTrustStatus.Trusted
        });

        _context.Applications.Add(new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Domain = "app.example.com",
            UpstreamUrl = "http://localhost",
            IsEnabled = true
        });

        _context.IpAddresses.Add(new IpAddress
        {
            Id = Guid.NewGuid(),
            Ip = "198.51.100.1",
            RequestCount = 5,
            ThreatScore = 10
        });

        _context.SecurityEvents.Add(new SecurityEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Type = SecurityEventType.AccessBlocked,
            Severity = SecurityEventSeverity.High,
            SourceIp = "198.51.100.1"
        });

        _context.BlocklistEntries.Add(new BlocklistEntry
        {
            Id = Guid.NewGuid(),
            Type = BlocklistEntryType.Ip,
            Value = "198.51.100.1",
            Reason = "Blocked"
        });

        await _context.SaveChangesAsync();

        var overview = await _service.GetOverviewAsync();

        Assert.Equal(5, overview.TotalRequests);
        Assert.Equal(1, overview.BlockedRequests);
        Assert.Equal(1, overview.ActiveBlocks);
        Assert.Equal(1, overview.SecurityEventsToday);
        Assert.Equal(1, overview.TotalUsers);
        Assert.Equal(1, overview.TotalDevices);
        Assert.Equal(1, overview.TotalApplications);
    }

    [Fact]
    public async Task GetTopThreatsAsync_ReturnsOrderedIps()
    {
        _context.IpAddresses.AddRange(
            new IpAddress { Id = Guid.NewGuid(), Ip = "198.51.100.1", ThreatScore = 50 },
            new IpAddress { Id = Guid.NewGuid(), Ip = "198.51.100.2", ThreatScore = 90 }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetTopThreatsAsync(2);

        Assert.Equal(2, result.Count);
        Assert.Equal("198.51.100.2", result[0].IpAddress);
    }

    [Fact]
    public async Task GetTopAttackTypesAsync_GroupsByType()
    {
        _context.WafEvents.AddRange(
            new WafEvent { Id = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, SourceIp = "1.1.1.1", RuleId = "1", Method = "GET", Uri = "/", AttackType = AttackType.SqlInjection },
            new WafEvent { Id = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, SourceIp = "1.1.1.1", RuleId = "2", Method = "GET", Uri = "/", AttackType = AttackType.SqlInjection },
            new WafEvent { Id = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, SourceIp = "1.1.1.1", RuleId = "3", Method = "GET", Uri = "/", AttackType = AttackType.CrossSiteScripting }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetTopAttackTypesAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("SqlInjection", result[0].Type);
        Assert.Equal(2, result[0].Count);
    }

    [Fact]
    public async Task GetRecentEventsAsync_ReturnsLatestFirst()
    {
        _context.SecurityEvents.AddRange(
            new SecurityEvent { Id = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5), Type = SecurityEventType.AuthenticationFailure, Severity = SecurityEventSeverity.Low, SourceIp = "1.1.1.1" },
            new SecurityEvent { Id = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, Type = SecurityEventType.RateLimitExceeded, Severity = SecurityEventSeverity.Medium, SourceIp = "1.1.1.2" }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetRecentEventsAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("RateLimitExceeded", result[0].EventType);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
