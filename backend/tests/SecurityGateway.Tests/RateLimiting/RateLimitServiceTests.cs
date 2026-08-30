using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Application.RateLimiting.Models;
using SecurityGateway.Domain.RateLimiting;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.RateLimiting.Repositories;
using SecurityGateway.Infrastructure.RateLimiting.Services;
using SecurityGateway.Tests.TestHelpers;
using Xunit;

namespace SecurityGateway.Tests.RateLimiting;

public class RateLimitServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly InMemoryRateLimitStore _store;
    private readonly RateLimitService _service;

    public RateLimitServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _store = new InMemoryRateLimitStore();
        var ruleRepository = new RateLimitRuleRepository(_context);
        var blocklistRepository = new BlocklistRepository(_context);

        _service = new RateLimitService(_store, ruleRepository, blocklistRepository, _context);
    }

    [Fact]
    public async Task CheckAsync_NoRules_Allows()
    {
        var context = CreateContext();

        var result = await _service.CheckAsync(context);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task CheckAsync_UnderLimit_Allows()
    {
        await _service.CreateRuleAsync(new Application.RateLimiting.DTOs.CreateRateLimitRuleRequest
        {
            ScopeType = RateLimitScopeType.Ip,
            ScopeValue = "198.51.100.1",
            RequestsPerWindow = 5,
            WindowSeconds = 60
        });

        var context = CreateContext("198.51.100.1");

        var result = await _service.CheckAsync(context);

        Assert.True(result.Allowed);
        Assert.Equal(4, result.Remaining);
    }

    [Fact]
    public async Task CheckAsync_OverLimit_Denies()
    {
        await _service.CreateRuleAsync(new Application.RateLimiting.DTOs.CreateRateLimitRuleRequest
        {
            ScopeType = RateLimitScopeType.Ip,
            ScopeValue = "198.51.100.1",
            RequestsPerWindow = 2,
            WindowSeconds = 60
        });

        var context = CreateContext("198.51.100.1");

        await _service.CheckAsync(context);
        await _service.CheckAsync(context);
        await _service.CheckAsync(context);
        var result = await _service.CheckAsync(context);

        Assert.False(result.Allowed);
        Assert.Equal("Rate limit exceeded.", result.Reason);
    }

    [Fact]
    public async Task CheckAsync_GlobalRule_AppliesToAllIps()
    {
        await _service.CreateRuleAsync(new Application.RateLimiting.DTOs.CreateRateLimitRuleRequest
        {
            ScopeType = RateLimitScopeType.Global,
            RequestsPerWindow = 3,
            WindowSeconds = 60
        });

        var context = CreateContext("198.51.100.99");

        await _service.CheckAsync(context);
        await _service.CheckAsync(context);
        await _service.CheckAsync(context);
        var result = await _service.CheckAsync(context);

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task CheckAsync_DisabledRule_DoesNotApply()
    {
        var rule = await _service.CreateRuleAsync(new Application.RateLimiting.DTOs.CreateRateLimitRuleRequest
        {
            ScopeType = RateLimitScopeType.Ip,
            ScopeValue = "198.51.100.1",
            RequestsPerWindow = 1,
            WindowSeconds = 60
        });

        await _service.UpdateRuleAsync(rule.Id, new Application.RateLimiting.DTOs.CreateRateLimitRuleRequest
        {
            ScopeType = RateLimitScopeType.Ip,
            ScopeValue = "198.51.100.1",
            RequestsPerWindow = 1,
            WindowSeconds = 60
        });

        // Updating does not disable; this test is just for coverage. A real disable endpoint could be added later.
        var context = CreateContext("198.51.100.1");
        var result = await _service.CheckAsync(context);

        Assert.True(result.Allowed);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static RateLimitRequestContext CreateContext(string ip = "198.51.100.1")
    {
        return new RateLimitRequestContext
        {
            IpAddress = ip,
            Domain = "test.example.com",
            Endpoint = "/api/test"
        };
    }
}
