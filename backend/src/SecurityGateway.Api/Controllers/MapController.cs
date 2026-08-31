using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Map;
using SecurityGateway.Application.Map.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class MapController : ControllerBase
{
    private readonly IMapService _mapService;

    public MapController(IMapService mapService)
    {
        _mapService = mapService;
    }

    [HttpGet("points")]
    public async Task<IActionResult> GetPoints([FromQuery] MapFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mapService.GetPointsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("attacks")]
    public async Task<IActionResult> GetAttackPoints([FromQuery] MapFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mapService.GetAttackPointsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ip/{ipAddress}")]
    public async Task<IActionResult> GetIpDetails(string ipAddress, CancellationToken cancellationToken)
    {
        var result = await _mapService.GetIpDetailsAsync(ipAddress, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken cancellationToken)
    {
        var result = await _mapService.GetCountriesAsync(cancellationToken);
        return Ok(result);
    }
}
