using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Audit.DTOs;
using SecurityGateway.Domain.Audit;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Audit.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
        => _context.AuditLogs.AddAsync(log, cancellationToken).AsTask();

    public async Task<IReadOnlyList<AuditLog>> SearchAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return logs;
    }

    public Task<long> CountAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);
        return query.LongCountAsync(cancellationToken);
    }

    private IQueryable<AuditLog> BuildQuery(AuditLogFilterRequest filter)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (filter.Category.HasValue)
        {
            query = query.Where(l => l.Category == filter.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(l => l.Action.Contains(filter.Action));
        }

        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            query = query.Where(l => l.Username != null && l.Username.Contains(filter.Username));
        }

        if (!string.IsNullOrWhiteSpace(filter.IpAddress))
        {
            query = query.Where(l => l.IpAddress != null && l.IpAddress.Contains(filter.IpAddress));
        }

        if (filter.Success.HasValue)
        {
            query = query.Where(l => l.Success == filter.Success.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(l => l.Timestamp >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(l => l.Timestamp <= filter.To.Value);
        }

        return query;
    }
}
