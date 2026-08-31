using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Notifications.Repositories;

public class NotificationChannelRepository : INotificationChannelRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationChannelRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<NotificationChannel>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.NotificationChannels
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationChannel>)t.Result, cancellationToken);

    public Task<IReadOnlyList<NotificationChannel>> GetEnabledAsync(CancellationToken cancellationToken = default)
        => _context.NotificationChannels
            .Where(c => c.IsEnabled)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationChannel>)t.Result, cancellationToken);

    public Task<NotificationChannel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.NotificationChannels.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task AddAsync(NotificationChannel channel, CancellationToken cancellationToken = default)
        => _context.NotificationChannels.AddAsync(channel, cancellationToken).AsTask();

    public void Update(NotificationChannel channel)
    {
        _context.NotificationChannels.Update(channel);
    }

    public void Delete(NotificationChannel channel)
    {
        _context.NotificationChannels.Remove(channel);
    }
}
