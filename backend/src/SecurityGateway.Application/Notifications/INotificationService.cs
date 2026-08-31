using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationChannelDto>> GetChannelsAsync(CancellationToken cancellationToken = default);
    Task<NotificationChannelDto?> GetChannelByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationChannelDto> CreateChannelAsync(CreateNotificationChannelRequest request, CancellationToken cancellationToken = default);
    Task<NotificationChannelDto> UpdateChannelAsync(Guid id, CreateNotificationChannelRequest request, CancellationToken cancellationToken = default);
    Task DeleteChannelAsync(Guid id, CancellationToken cancellationToken = default);
    Task SendTestAsync(Guid channelId, SendTestNotificationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationLog>> GetRecentLogsAsync(int limit, CancellationToken cancellationToken = default);
}
