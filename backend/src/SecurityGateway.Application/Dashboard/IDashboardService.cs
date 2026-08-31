using SecurityGateway.Application.Dashboard.DTOs;

namespace SecurityGateway.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEventSeriesDto>> GetSecurityEventSeriesAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopThreatDto>> GetTopThreatsAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttackTypeDto>> GetTopAttackTypesAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentEventDto>> GetRecentEventsAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentEventDto>> GetTimelineAsync(DateTime from, DateTime to, int limit, CancellationToken cancellationToken = default);
}
