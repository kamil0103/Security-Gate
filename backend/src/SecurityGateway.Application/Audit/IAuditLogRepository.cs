using SecurityGateway.Application.Audit.DTOs;
using SecurityGateway.Domain.Audit;

namespace SecurityGateway.Application.Audit;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> SearchAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);
    Task<long> CountAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);
}
