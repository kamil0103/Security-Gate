using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.ThreatDetection.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class ThreatScoreRulesController : ControllerBase
{
    private readonly IThreatDetectionService _threatDetectionService;

    public ThreatScoreRulesController(IThreatDetectionService threatDetectionService)
    {
        _threatDetectionService = threatDetectionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules(CancellationToken cancellationToken)
    {
        var rules = await _threatDetectionService.GetRulesAsync(cancellationToken);
        return Ok(rules);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRuleById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _threatDetectionService.GetRuleByIdAsync(id, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateThreatScoreRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _threatDetectionService.CreateRuleAsync(request, cancellationToken);
        return Created($"/api/threatscorerules/{rule.Id}", rule);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] CreateThreatScoreRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _threatDetectionService.UpdateRuleAsync(id, request, cancellationToken);
        return Ok(rule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken cancellationToken)
    {
        await _threatDetectionService.DeleteRuleAsync(id, cancellationToken);
        return NoContent();
    }
}
