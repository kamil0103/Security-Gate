using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Cloudflare;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class CloudflareController : ControllerBase
{
    private readonly ICloudflareIpService _cloudflareIpService;
    private readonly CloudflareOptions _options;

    public CloudflareController(ICloudflareIpService cloudflareIpService, CloudflareOptions options)
    {
        _cloudflareIpService = cloudflareIpService;
        _options = options;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            _options.Enabled,
            _options.TrustConnectingIp,
            _options.TrustVisitorIp,
            IpRanges = _cloudflareIpService.GetRanges()
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshRanges(CancellationToken cancellationToken)
    {
        await _cloudflareIpService.RefreshRangesAsync(cancellationToken);
        return Ok(new { refreshed = true, ipRanges = _cloudflareIpService.GetRanges() });
    }

    [HttpGet("check")]
    public IActionResult CheckIp([FromQuery] string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BadRequest("IP address is required.");
        }

        return Ok(new { ipAddress, isCloudflare = _cloudflareIpService.IsCloudflareIp(ipAddress) });
    }
}
