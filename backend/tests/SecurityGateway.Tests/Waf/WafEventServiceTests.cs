using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Waf;
using SecurityGateway.Application.Waf.DTOs;
using SecurityGateway.Domain.Waf;
using SecurityGateway.Infrastructure.IpIntelligence.Repositories;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Waf.Repositories;
using SecurityGateway.Infrastructure.Waf.Services;
using SecurityGateway.Tests.TestHelpers;
using Xunit;

namespace SecurityGateway.Tests.Waf;

public class WafEventServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly WafEventService _service;

    public WafEventServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var wafEventRepository = new WafEventRepository(_context);
        var ipAddressRepository = new IpAddressRepository(_context);
        var classifier = new ModSecurityAttackClassifier();

        _service = new WafEventService(wafEventRepository, classifier, ipAddressRepository, new FakeThreatDetectionService(), _context);
    }

    [Fact]
    public async Task IngestAsync_ClassifiesAndStoresEvent()
    {
        var request = new CreateWafEventRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            SourceIp = "198.51.100.10",
            RuleId = "942100",
            RuleMessage = "SQL Injection Attack",
            Method = "GET",
            Uri = "/api/test?id=1' OR '1'='1",
            Action = WafAction.Blocked
        };

        var result = await _service.IngestAsync(request);

        Assert.NotNull(result);
        Assert.Equal(AttackType.SqlInjection, result.AttackType);
        Assert.Equal(AttackSeverity.Critical, result.Severity);
        Assert.Equal(WafAction.Blocked, result.Action);
    }

    [Fact]
    public async Task IngestAsync_KnownIp_IncrementsAttackCount()
    {
        var ip = new SecurityGateway.Domain.IpIntelligence.IpAddress
        {
            Ip = "198.51.100.20",
            AttackCount = 3
        };
        _context.IpAddresses.Add(ip);
        await _context.SaveChangesAsync();

        var request = new CreateWafEventRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            SourceIp = "198.51.100.20",
            RuleId = "941100",
            RuleMessage = "XSS Attack Detected",
            Method = "POST",
            Uri = "/api/test",
            Action = WafAction.Blocked
        };

        await _service.IngestAsync(request);

        var updatedIp = await _context.IpAddresses.FindAsync(ip.Id);
        Assert.Equal(4, updatedIp!.AttackCount);
        Assert.True(updatedIp.ThreatScore > 0);
    }

    [Fact]
    public async Task SearchAsync_FiltersBySourceIp()
    {
        await _service.IngestAsync(new CreateWafEventRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            SourceIp = "198.51.100.30",
            RuleId = "920100",
            Method = "GET",
            Uri = "/",
            Action = WafAction.Blocked
        });

        await _service.IngestAsync(new CreateWafEventRequest
        {
            Timestamp = DateTimeOffset.UtcNow,
            SourceIp = "198.51.100.31",
            RuleId = "920100",
            Method = "GET",
            Uri = "/",
            Action = WafAction.Blocked
        });

        var results = await _service.SearchAsync(new WafEventFilter { SourceIp = "198.51.100.30" });

        Assert.Single(results);
        Assert.Equal("198.51.100.30", results[0].SourceIp);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
