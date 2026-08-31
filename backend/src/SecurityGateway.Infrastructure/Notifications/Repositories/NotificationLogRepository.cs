using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Notifications.Repositories;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
        => _context.NotificationLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationLog>)t.Result, cancellationToken);

    public Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default)
        => _context.NotificationLogs.AddAsync(log, cancellationToken).AsTask();
}
