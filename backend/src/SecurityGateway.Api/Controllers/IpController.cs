using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.IpIntelligence;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IpController : ControllerBase
{
    private readonly IIpIntelligenceService _ipIntelligenceService;

    public IpController(IIpIntelligenceService ipIntelligenceService)
    {
        _ipIntelligenceService = ipIntelligenceService;
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 50, CancellationToken cancellationToken = default)
    {
        var ips = await _ipIntelligenceService.GetRecentAsync(count, cancellationToken);
        return Ok(ips);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ip = await _ipIntelligenceService.GetByIdAsync(id, cancellationToken);
        return ip is null ? NotFound() : Ok(ip);
    }

    [HttpGet("me")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyIp(CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _ipIntelligenceService.TrackAsync(new TrackIpRequest
        {
            IpAddress = ip,
            UserId = GetOptionalUserId()
        }, cancellationToken);

        return Ok(result);
    }

    private Guid? GetOptionalUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
