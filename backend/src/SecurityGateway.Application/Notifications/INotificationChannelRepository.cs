using SecurityGateway.Domain.Notifications;

namespace SecurityGateway.Application.Notifications;

public interface INotificationChannelRepository
{
    Task<IReadOnlyList<NotificationChannel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationChannel>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<NotificationChannel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationChannel channel, CancellationToken cancellationToken = default);
    void Update(NotificationChannel channel);
    void Delete(NotificationChannel channel);
}
