using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Dashboard;
using SecurityGateway.Application.Dashboard.DTOs;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var totalRequests = await _context.IpAddresses
            .SumAsync(ip => ip.RequestCount, cancellationToken)
            .ConfigureAwait(false);

        var blockedRequests = await _context.SecurityEvents
            .CountAsync(e => e.Type == SecurityEventType.AccessBlocked, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var activeBlocks = await _context.BlocklistEntries
            .CountAsync(e => e.Type == BlocklistEntryType.Ip && e.IsEnabled && (e.ExpiresAt == null || e.ExpiresAt > now), cancellationToken)
            .ConfigureAwait(false);

        var securityEventsToday = await _context.SecurityEvents
            .CountAsync(e => e.Timestamp >= today, cancellationToken)
            .ConfigureAwait(false);

        var wafEventsToday = await _context.WafEvents
            .CountAsync(e => e.Timestamp >= today, cancellationToken)
            .ConfigureAwait(false);

        var rateLimitHitsToday = await _context.SecurityEvents
            .CountAsync(e => e.Type == SecurityEventType.RateLimitExceeded && e.Timestamp >= today, cancellationToken)
            .ConfigureAwait(false);

        var totalApplications = await _context.Applications.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalDevices = await _context.Devices.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalUsers = await _context.Users.CountAsync(cancellationToken).ConfigureAwait(false);

        return new DashboardOverviewDto
        {
            TotalRequests = totalRequests,
            BlockedRequests = blockedRequests,
            ActiveBlocks = activeBlocks,
            SecurityEventsToday = securityEventsToday,
            WafEventsToday = wafEventsToday,
            RateLimitHitsToday = rateLimitHitsToday,
            TotalApplications = totalApplications,
            TotalDevices = totalDevices,
            TotalUsers = totalUsers
        };
    }

    public async Task<IReadOnlyList<SecurityEventSeriesDto>> GetSecurityEventSeriesAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var events = await _context.SecurityEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var severities = events.Select(e => e.Severity.ToString()).Distinct().ToList();
        var pointsBySeverity = events
            .GroupBy(e => new { Severity = e.Severity.ToString(), Date = e.Timestamp.DateTime.Date })
            .Select(g => new { g.Key.Severity, g.Key.Date, Count = g.LongCount() })
            .ToList();

        var result = severities
            .Select(severity => new SecurityEventSeriesDto
            {
                Severity = severity,
                Points = pointsBySeverity
                    .Where(p => p.Severity == severity)
                    .OrderBy(p => p.Date)
                    .Select(p => new TimeSeriesPointDto { Timestamp = p.Date, Count = p.Count })
                    .ToList()
            })
            .ToList();

        return result;
    }

    public async Task<IReadOnlyList<TopThreatDto>> GetTopThreatsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var ips = await _context.IpAddresses
            .Where(ip => ip.ThreatScore > 0)
            .OrderByDescending(ip => ip.ThreatScore)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ips
            .Select(ip => new TopThreatDto
            {
                IpAddress = ip.Ip,
                ThreatScore = ip.ThreatScore,
                RequestCount = ip.RequestCount,
                AttackCount = ip.AttackCount
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AttackTypeDto>> GetTopAttackTypesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var attackTypes = await _context.WafEvents
            .Where(e => e.AttackType != default)
            .GroupBy(e => e.AttackType.ToString())
            .Select(g => new AttackTypeDto { Type = g.Key, Count = g.LongCount() })
            .OrderByDescending(a => a.Count)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return attackTypes;
    }

    public async Task<IReadOnlyList<RecentEventDto>> GetRecentEventsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var events = await _context.SecurityEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return events.Select(MapSecurityEvent).ToList();
    }

    public async Task<IReadOnlyList<RecentEventDto>> GetTimelineAsync(DateTime from, DateTime to, int limit, CancellationToken cancellationToken = default)
    {
        var events = await _context.SecurityEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return events.Select(MapSecurityEvent).ToList();
    }

    private static RecentEventDto MapSecurityEvent(SecurityEvent e)
    {
        return new RecentEventDto
        {
            Id = e.Id,
            EventType = e.Type.ToString(),
            Severity = e.Severity.ToString(),
            SourceIp = e.SourceIp ?? "unknown",
            Description = e.Description,
            Timestamp = e.Timestamp.DateTime
        };
    }
}
