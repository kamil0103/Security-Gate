using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.Notifications.Providers;
using SecurityGateway.Infrastructure.Notifications.Repositories;
using SecurityGateway.Infrastructure.Notifications.Services;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.Notifications;

public class NotificationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _service;
    private readonly FakeEmailService _emailService;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _emailService = new FakeEmailService();
        var providers = new List<INotificationChannelProvider>
        {
            new EmailNotificationProvider(_emailService)
        };

        _service = new NotificationService(
            new NotificationChannelRepository(_context),
            new NotificationLogRepository(_context),
            providers,
            _context);
    }

    [Fact]
    public async Task CreateChannelAsync_AddsChannel()
    {
        var request = new CreateNotificationChannelRequest
        {
            Name = "Admin Email",
            Type = NotificationChannelType.Email,
            Configuration = "{\"To\":\"admin@example.com\"}"
        };

        var result = await _service.CreateChannelAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Admin Email", result.Name);
    }

    [Fact]
    public async Task SendTestAsync_EmailProvider_SendsEmailAndLogs()
    {
        var channel = await _service.CreateChannelAsync(new CreateNotificationChannelRequest
        {
            Name = "Admin Email",
            Type = NotificationChannelType.Email,
            Configuration = "{\"To\":\"admin@example.com\"}"
        });

        await _service.SendTestAsync(channel.Id, new SendTestNotificationRequest
        {
            Subject = "Test",
            Body = "Test body"
        });

        Assert.Single(_emailService.SentEmails);
        Assert.Equal("admin@example.com", _emailService.SentEmails[0].To);

        var logs = await _service.GetRecentLogsAsync(10);
        Assert.Single(logs);
        Assert.Equal(NotificationStatus.Sent, logs[0].Status);
    }

    [Fact]
    public async Task DeleteChannelAsync_RemovesChannel()
    {
        var channel = await _service.CreateChannelAsync(new CreateNotificationChannelRequest
        {
            Name = "Admin Email",
            Type = NotificationChannelType.Email,
            Configuration = "{}"
        });

        await _service.DeleteChannelAsync(channel.Id);

        var result = await _service.GetChannelByIdAsync(channel.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task Dispatcher_SendsOnlyForHighSeverity()
    {
        var channel = await _service.CreateChannelAsync(new CreateNotificationChannelRequest
        {
            Name = "Admin Email",
            Type = NotificationChannelType.Email,
            Configuration = "{\"To\":\"admin@example.com\"}"
        });

        var dispatcher = new NotificationDispatcher(
            new NotificationChannelRepository(_context),
            new List<INotificationChannelProvider> { new EmailNotificationProvider(_emailService) },
            new NotificationLogRepository(_context),
            _context);

        await dispatcher.DispatchAsync(new SecurityEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Type = SecurityEventType.AccessBlocked,
            Severity = SecurityEventSeverity.Low,
            SourceIp = "1.1.1.1"
        });

        Assert.Empty(_emailService.SentEmails);

        await dispatcher.DispatchAsync(new SecurityEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Type = SecurityEventType.AccessBlocked,
            Severity = SecurityEventSeverity.High,
            SourceIp = "1.1.1.1"
        });

        Assert.Single(_emailService.SentEmails);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

        public bool IsConfigured => true;

        public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            SentEmails.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }
}
