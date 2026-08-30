using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Health;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckAsync(cancellationToken).ConfigureAwait(false);

        if (result.Status == "Healthy")
        {
            return Ok(result);
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
