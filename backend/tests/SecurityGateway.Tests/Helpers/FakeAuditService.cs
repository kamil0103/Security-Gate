using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Audit.DTOs;
using SecurityGateway.Domain.Audit;

namespace SecurityGateway.Tests.Helpers;

public sealed class FakeAuditService : IAuditService
{
    public List<AuditLog> Logs { get; } = new();

    public Task LogAsync(
        AuditCategory category,
        string action,
        Guid? userId = null,
        string? username = null,
        string? ipAddress = null,
        string? details = null,
        bool success = true,
        CancellationToken cancellationToken = default)
    {
        Logs.Add(new AuditLog
        {
            Category = category,
            Action = action,
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            Details = details,
            Success = success
        });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditLogDto>> SearchAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AuditLogDto>>(new List<AuditLogDto>());

    public Task<long> CountAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
        => Task.FromResult(0L);
}
