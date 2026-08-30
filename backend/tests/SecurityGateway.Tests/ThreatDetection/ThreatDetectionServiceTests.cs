using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.ThreatDetection.DTOs;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.IpIntelligence.Repositories;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.ThreatDetection.Repositories;
using SecurityGateway.Infrastructure.ThreatDetection.Services;
using Xunit;

namespace SecurityGateway.Tests.ThreatDetection;

public class ThreatDetectionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ThreatDetectionService _service;

    public ThreatDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var eventRepository = new SecurityEventRepository(_context);
        var ruleRepository = new ThreatScoreRuleRepository(_context);
        var ipAddressRepository = new IpAddressRepository(_context);

        _service = new ThreatDetectionService(eventRepository, ruleRepository, ipAddressRepository, _context);
    }

    [Fact]
    public async Task RecordEventAsync_StoresEvent()
    {
        var request = new CreateSecurityEventRequest
        {
            Type = SecurityEventType.AuthenticationFailure,
            Severity = SecurityEventSeverity.Low,
            SourceIp = "198.51.100.1",
            Description = "Failed login"
        };

        var result = await _service.RecordEventAsync(request);

        Assert.NotNull(result);
        Assert.Equal(SecurityEventType.AuthenticationFailure, result.Type);
        Assert.Equal("198.51.100.1", result.SourceIp);
    }

    [Fact]
    public async Task CreateRuleAsync_StoresRule()
    {
        var request = new CreateThreatScoreRuleRequest
        {
            Name = "Brute Force",
            EventType = SecurityEventType.AuthenticationFailure,
            EventCountThreshold = 5,
            TimeWindowSeconds = 300,
            ScoreImpact = 20
        };

        var result = await _service.CreateRuleAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Brute Force", result.Name);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task EvaluateThreatScoreAsync_MatchingRule_EscalatesScore()
    {
        await _service.CreateRuleAsync(new CreateThreatScoreRuleRequest
        {
            Name = "Brute Force",
            EventType = SecurityEventType.AuthenticationFailure,
            EventCountThreshold = 3,
            TimeWindowSeconds = 300,
            ScoreImpact = 25
        });

        for (var i = 0; i < 3; i++)
        {
            await _service.RecordEventAsync(new CreateSecurityEventRequest
            {
                Type = SecurityEventType.AuthenticationFailure,
                Severity = SecurityEventSeverity.Low,
                SourceIp = "198.51.100.1"
            });
        }

        var result = await _service.EvaluateThreatScoreAsync("198.51.100.1");

        Assert.NotNull(result);
        Assert.True(result.Escalated);
        Assert.True(result.NewScore >= 25);
        Assert.False(string.IsNullOrWhiteSpace(result.ThreatLevel));
    }

    [Fact]
    public async Task EvaluateThreatScoreAsync_NoMatchingRule_ReturnsNull()
    {
        var result = await _service.EvaluateThreatScoreAsync("198.51.100.1");
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchEventsAsync_FiltersByType()
    {
        await _service.RecordEventAsync(new CreateSecurityEventRequest
        {
            Type = SecurityEventType.AuthenticationFailure,
            Severity = SecurityEventSeverity.Low,
            SourceIp = "198.51.100.1"
        });

        await _service.RecordEventAsync(new CreateSecurityEventRequest
        {
            Type = SecurityEventType.RateLimitExceeded,
            Severity = SecurityEventSeverity.Medium,
            SourceIp = "198.51.100.1"
        });

        var results = await _service.SearchEventsAsync(new SecurityEventFilter { Type = SecurityEventType.RateLimitExceeded });

        Assert.Single(results);
        Assert.Equal(SecurityEventType.RateLimitExceeded, results[0].Type);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
