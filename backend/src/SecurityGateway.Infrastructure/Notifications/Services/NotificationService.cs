using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Audit;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationChannelRepository _channelRepository;
    private readonly INotificationLogRepository _logRepository;
    private readonly IEnumerable<INotificationChannelProvider> _providers;
    private readonly IAuditService _auditService;
    private readonly ApplicationDbContext _context;

    public NotificationService(
        INotificationChannelRepository channelRepository,
        INotificationLogRepository logRepository,
        IEnumerable<INotificationChannelProvider> providers,
        IAuditService auditService,
        ApplicationDbContext context)
    {
        _channelRepository = channelRepository;
        _logRepository = logRepository;
        _providers = providers;
        _auditService = auditService;
        _context = context;
    }

    public async Task<IReadOnlyList<NotificationChannelDto>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        var channels = await _channelRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return channels.Select(MapChannel).ToList();
    }

    public async Task<NotificationChannelDto?> GetChannelByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return channel is null ? null : MapChannel(channel);
    }

    public async Task<NotificationChannelDto> CreateChannelAsync(CreateNotificationChannelRequest request, CancellationToken cancellationToken = default)
    {
        var channel = new NotificationChannel
        {
            Name = request.Name,
            Type = request.Type,
            IsEnabled = request.IsEnabled,
            Configuration = request.Configuration
        };

        await _channelRepository.AddAsync(channel, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.Notification,
            "CreateNotificationChannel",
            null,
            null,
            null,
            $"Created {channel.Type} notification channel {channel.Name}",
            true,
            cancellationToken).ConfigureAwait(false);

        return MapChannel(channel);
    }

    public async Task<NotificationChannelDto> UpdateChannelAsync(Guid id, CreateNotificationChannelRequest request, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Channel not found.");

        channel.Name = request.Name;
        channel.Type = request.Type;
        channel.IsEnabled = request.IsEnabled;
        channel.Configuration = request.Configuration;

        _channelRepository.Update(channel);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.Notification,
            "UpdateNotificationChannel",
            null,
            null,
            null,
            $"Updated {channel.Type} notification channel {channel.Name}",
            true,
            cancellationToken).ConfigureAwait(false);

        return MapChannel(channel);
    }

    public async Task DeleteChannelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Channel not found.");

        _channelRepository.Delete(channel);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.Notification,
            "DeleteNotificationChannel",
            null,
            null,
            null,
            $"Deleted {channel.Type} notification channel {channel.Name}",
            true,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SendTestAsync(Guid channelId, SendTestNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Channel not found.");

        var message = new NotificationMessage
        {
            Title = request.Subject,
            Body = request.Body,
            Severity = SecurityEventSeverity.Info,
            Timestamp = DateTimeOffset.UtcNow
        };

        await SendToChannelAsync(channel, message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<NotificationLog>> GetRecentLogsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _logRepository.GetRecentAsync(limit, cancellationToken).ConfigureAwait(false);
    }

    internal async Task SendToChannelAsync(NotificationChannel channel, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var provider = _providers.FirstOrDefault(p => p.CanHandle(channel.Type));

        if (provider is null)
        {
            throw new InvalidOperationException($"No provider registered for channel type {channel.Type}.");
        }

        var log = new NotificationLog
        {
            ChannelId = channel.Id,
            ChannelType = channel.Type,
            Recipient = channel.Name,
            Subject = message.Title,
            Body = message.Body,
            Status = NotificationStatus.Pending
        };

        await _logRepository.AddAsync(log, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await provider.SendAsync(channel, message, cancellationToken).ConfigureAwait(false);

            log.Status = NotificationStatus.Sent;
            log.SentAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            log.Status = NotificationStatus.Failed;
            log.ErrorMessage = ex.Message;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static NotificationChannelDto MapChannel(NotificationChannel channel)
    {
        return new NotificationChannelDto
        {
            Id = channel.Id,
            Name = channel.Name,
            Type = channel.Type,
            IsEnabled = channel.IsEnabled,
            Configuration = channel.Configuration
        };
    }
}
