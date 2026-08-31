using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Blocking.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class BlockingController : ControllerBase
{
    private readonly IAutomaticBlockingService _automaticBlockingService;

    public BlockingController(IAutomaticBlockingService automaticBlockingService)
    {
        _automaticBlockingService = automaticBlockingService;
    }

    [HttpPost("block")]
    public async Task<IActionResult> Block([FromBody] BlockIpRequest request, CancellationToken cancellationToken)
    {
        var result = await _automaticBlockingService.BlockAsync(request.IpAddress, request.DurationMinutes, request.Reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("unblock")]
    public async Task<IActionResult> Unblock([FromBody] BlockIpRequest request, CancellationToken cancellationToken)
    {
        await _automaticBlockingService.UnblockAsync(request.IpAddress, cancellationToken);
        return NoContent();
    }

    [HttpGet("is-blocked")]
    public async Task<IActionResult> IsBlocked([FromQuery] string ipAddress, CancellationToken cancellationToken)
    {
        var isBlocked = await _automaticBlockingService.IsBlockedAsync(ipAddress, cancellationToken);
        return Ok(new { IpAddress = ipAddress, IsBlocked = isBlocked });
    }
}
