using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.Blocking.Services;
using SecurityGateway.Infrastructure.Identity;
using SecurityGateway.Infrastructure.IpIntelligence.Repositories;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using SecurityGateway.Tests.Helpers;
using Xunit;

namespace SecurityGateway.Tests.Blocking;

public class AutomaticBlockingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AutomaticBlockingOptions _options;
    private readonly AutomaticBlockingService _service;

    public AutomaticBlockingServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(dbOptions);
        _context.Database.EnsureCreated();

        _options = new AutomaticBlockingOptions
        {
            Enabled = true,
            MediumThreshold = 40,
            HighThreshold = 60,
            CriticalThreshold = 80,
            MediumBlockDurationMinutes = 30,
            HighBlockDurationMinutes = 240,
            CriticalBlockDurationMinutes = 1440
        };

        var blocklistRepository = new BlocklistRepository(_context);
        var ipAddressRepository = new IpAddressRepository(_context);

        _service = new AutomaticBlockingService(blocklistRepository, ipAddressRepository, _context, _options, new FakeAuditService());
    }

    [Fact]
    public async Task CheckAndBlockAsync_Disabled_ReturnsNull()
    {
        var options = new AutomaticBlockingOptions { Enabled = false };
        var service = new AutomaticBlockingService(new BlocklistRepository(_context), new IpAddressRepository(_context), _context, options, new FakeAuditService());

        var result = await service.CheckAndBlockAsync("198.51.100.1", 100);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckAndBlockAsync_BelowThreshold_ReturnsNull()
    {
        var result = await _service.CheckAndBlockAsync("198.51.100.1", 20);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckAndBlockAsync_MediumThreshold_Blocks()
    {
        var result = await _service.CheckAndBlockAsync("198.51.100.1", 50);

        Assert.NotNull(result);
        Assert.True(result.Blocked);
        Assert.Equal("198.51.100.1", result.IpAddress);
        Assert.NotNull(result.ExpiresAt);
    }

    [Fact]
    public async Task BlockAsync_PermanentBlock_NoExpiry()
    {
        var result = await _service.BlockAsync("198.51.100.2", null, "Manual block");

        Assert.True(result.Blocked);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public async Task UnblockAsync_RemovesBlock()
    {
        await _service.BlockAsync("198.51.100.3", 60, "Test");

        await _service.UnblockAsync("198.51.100.3");

        var isBlocked = await _service.IsBlockedAsync("198.51.100.3");
        Assert.False(isBlocked);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
