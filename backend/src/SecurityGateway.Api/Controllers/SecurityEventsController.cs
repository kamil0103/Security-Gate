using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.ThreatDetection.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class SecurityEventsController : ControllerBase
{
    private readonly IThreatDetectionService _threatDetectionService;

    public SecurityEventsController(IThreatDetectionService threatDetectionService)
    {
        _threatDetectionService = threatDetectionService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SecurityEventFilter filter, CancellationToken cancellationToken)
    {
        var events = await _threatDetectionService.SearchEventsAsync(filter, cancellationToken);
        return Ok(events);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 50, CancellationToken cancellationToken = default)
    {
        var events = await _threatDetectionService.GetRecentEventsAsync(count, cancellationToken);
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var securityEvent = await _threatDetectionService.GetEventByIdAsync(id, cancellationToken);
        return securityEvent is null ? NotFound() : Ok(securityEvent);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSecurityEventRequest request, CancellationToken cancellationToken)
    {
        var securityEvent = await _threatDetectionService.RecordEventAsync(request, cancellationToken);
        return Created($"/api/securityevents/{securityEvent.Id}", securityEvent);
    }
}
