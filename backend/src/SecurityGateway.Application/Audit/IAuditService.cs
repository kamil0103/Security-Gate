using SecurityGateway.Application.Audit.DTOs;
using SecurityGateway.Domain.Audit;

namespace SecurityGateway.Application.Audit;

public interface IAuditService
{
    Task LogAsync(AuditCategory category, string action, Guid? userId = null, string? username = null, string? ipAddress = null, string? details = null, bool success = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> SearchAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);
    Task<long> CountAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);
}
