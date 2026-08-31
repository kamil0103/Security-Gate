using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.BehavioralAnalysis;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class BehavioralAnalysisController : ControllerBase
{
    private readonly IBehavioralAnalysisService _behavioralAnalysisService;

    public BehavioralAnalysisController(IBehavioralAnalysisService behavioralAnalysisService)
    {
        _behavioralAnalysisService = behavioralAnalysisService;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] BehavioralRequest request, CancellationToken cancellationToken)
    {
        var result = await _behavioralAnalysisService.AnalyzeAsync(request, cancellationToken);
        return Ok(result);
    }
}
