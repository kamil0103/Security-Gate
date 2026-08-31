using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.ThreatIntelligence;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class ThreatIntelligenceController : ControllerBase
{
    private readonly IThreatIntelligenceService _threatIntelligenceService;

    public ThreatIntelligenceController(IThreatIntelligenceService threatIntelligenceService)
    {
        _threatIntelligenceService = threatIntelligenceService;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BadRequest("IP address is required.");
        }

        var results = await _threatIntelligenceService.LookupAsync(ipAddress, cancellationToken);
        return Ok(results);
    }
}
