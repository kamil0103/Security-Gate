using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Application.ThreatDetection;

public interface ISecurityEventRepository
{
    Task<SecurityEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> SearchAsync(
        SecurityEventType? type = null,
        SecurityEventSeverity? severity = null,
        string? sourceIp = null,
        Guid? userId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
    Task<int> CountEventsAsync(string sourceIp, SecurityEventType type, DateTimeOffset from, CancellationToken cancellationToken = default);
}
