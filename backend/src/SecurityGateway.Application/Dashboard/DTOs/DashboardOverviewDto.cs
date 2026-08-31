namespace SecurityGateway.Application.Dashboard.DTOs;

public class DashboardOverviewDto
{
    public long TotalRequests { get; set; }
    public long BlockedRequests { get; set; }
    public long ActiveBlocks { get; set; }
    public long SecurityEventsToday { get; set; }
    public long WafEventsToday { get; set; }
    public long RateLimitHitsToday { get; set; }
    public int TotalApplications { get; set; }
    public int TotalDevices { get; set; }
    public long TotalUsers { get; set; }
}
