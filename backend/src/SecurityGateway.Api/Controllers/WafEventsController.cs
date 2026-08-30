using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Waf;
using SecurityGateway.Application.Waf.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/waf-events")]
public class WafEventsController : ControllerBase
{
    private readonly IWafEventService _wafEventService;

    public WafEventsController(IWafEventService wafEventService)
    {
        _wafEventService = wafEventService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Ingest([FromBody] CreateWafEventRequest request, CancellationToken cancellationToken)
    {
        var wafEvent = await _wafEventService.IngestAsync(request, cancellationToken);
        return Created($"/api/waf-events/{wafEvent.Id}", wafEvent);
    }

    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Search([FromQuery] WafEventFilter filter, CancellationToken cancellationToken)
    {
        var events = await _wafEventService.SearchAsync(filter, cancellationToken);
        return Ok(events);
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 50, CancellationToken cancellationToken = default)
    {
        var events = await _wafEventService.GetRecentAsync(count, cancellationToken);
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var wafEvent = await _wafEventService.GetByIdAsync(id, cancellationToken);
        return wafEvent is null ? NotFound() : Ok(wafEvent);
    }
}
