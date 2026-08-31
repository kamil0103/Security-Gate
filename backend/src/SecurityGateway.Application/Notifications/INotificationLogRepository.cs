using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Application.Notifications;

public interface INotificationLogRepository
{
    Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default);
}
