using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Dashboard;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetOverviewAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("security-events-series")]
    public async Task<IActionResult> GetSecurityEventSeries([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var start = from ?? DateTime.UtcNow.AddDays(-7);
        var end = to ?? DateTime.UtcNow;
        var result = await _dashboardService.GetSecurityEventSeriesAsync(start, end, cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-threats")]
    public async Task<IActionResult> GetTopThreats([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetTopThreatsAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-attacks")]
    public async Task<IActionResult> GetTopAttacks([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetTopAttackTypesAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("recent-events")]
    public async Task<IActionResult> GetRecentEvents([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetRecentEventsAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var start = from ?? DateTime.UtcNow.AddDays(-1);
        var end = to ?? DateTime.UtcNow;
        var result = await _dashboardService.GetTimelineAsync(start, end, limit, cancellationToken);
        return Ok(result);
    }
}
