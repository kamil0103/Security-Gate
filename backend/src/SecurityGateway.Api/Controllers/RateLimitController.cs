using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Application.RateLimiting.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class RateLimitController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;

    public RateLimitController(IRateLimitService rateLimitService)
    {
        _rateLimitService = rateLimitService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules(CancellationToken cancellationToken)
    {
        var rules = await _rateLimitService.GetRulesAsync(cancellationToken);
        return Ok(rules);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRuleById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _rateLimitService.GetRuleByIdAsync(id, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateRateLimitRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _rateLimitService.CreateRuleAsync(request, cancellationToken);
        return Created($"/api/ratelimit/{rule.Id}", rule);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] CreateRateLimitRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _rateLimitService.UpdateRuleAsync(id, request, cancellationToken);
        return Ok(rule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken cancellationToken)
    {
        await _rateLimitService.DeleteRuleAsync(id, cancellationToken);
        return NoContent();
    }
}
