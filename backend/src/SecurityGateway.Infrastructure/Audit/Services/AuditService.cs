using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Audit.DTOs;
using SecurityGateway.Domain.Audit;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Audit.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ApplicationDbContext _context;

    public AuditService(IAuditLogRepository auditLogRepository, ApplicationDbContext context)
    {
        _auditLogRepository = auditLogRepository;
        _context = context;
    }

    public async Task LogAsync(
        AuditCategory category,
        string action,
        Guid? userId = null,
        string? username = null,
        string? ipAddress = null,
        string? details = null,
        bool success = true,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Category = category,
            Action = action,
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            Details = details,
            Success = success
        };

        await _auditLogRepository.AddAsync(log, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditLogDto>> SearchAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogRepository.SearchAsync(filter, cancellationToken).ConfigureAwait(false);
        return logs.Select(MapLog).ToList();
    }

    public Task<long> CountAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
    {
        return _auditLogRepository.CountAsync(filter, cancellationToken);
    }

    private static AuditLogDto MapLog(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            Category = log.Category,
            Action = log.Action,
            UserId = log.UserId,
            Username = log.Username,
            IpAddress = log.IpAddress,
            Details = log.Details,
            Success = log.Success
        };
    }
}
