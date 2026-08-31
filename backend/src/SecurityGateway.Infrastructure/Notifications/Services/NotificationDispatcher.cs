using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Notifications.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationChannelRepository _channelRepository;
    private readonly IEnumerable<INotificationChannelProvider> _providers;
    private readonly INotificationLogRepository _logRepository;
    private readonly ApplicationDbContext _context;

    public NotificationDispatcher(
        INotificationChannelRepository channelRepository,
        IEnumerable<INotificationChannelProvider> providers,
        INotificationLogRepository logRepository,
        ApplicationDbContext context)
    {
        _channelRepository = channelRepository;
        _providers = providers;
        _logRepository = logRepository;
        _context = context;
    }

    public async Task DispatchAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        if (securityEvent.Severity < SecurityEventSeverity.High)
        {
            return;
        }

        var channels = await _channelRepository.GetEnabledAsync(cancellationToken).ConfigureAwait(false);

        var message = new NotificationMessage
        {
            Title = $"Security Alert: {securityEvent.Type}",
            Body = securityEvent.Description ?? $"A {securityEvent.Severity} severity security event was detected.",
            Severity = securityEvent.Severity,
            SourceIp = securityEvent.SourceIp,
            EventType = securityEvent.Type.ToString(),
            Timestamp = securityEvent.Timestamp
        };

        foreach (var channel in channels)
        {
            var provider = _providers.FirstOrDefault(p => p.CanHandle(channel.Type));
            if (provider is null)
            {
                continue;
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
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
