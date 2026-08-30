using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Identity;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IDeviceIdentityService _deviceIdentityService;

    public DevicesController(IDeviceIdentityService deviceIdentityService)
    {
        _deviceIdentityService = deviceIdentityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyDevices(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var devices = await _deviceIdentityService.GetUserDevicesAsync(userId, cancellationToken);
        return Ok(devices);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingDevices(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var devices = await _deviceIdentityService.GetPendingDevicesAsync(userId, cancellationToken);
        return Ok(devices);
    }

    [HttpPost("{deviceId:guid}/trust")]
    public async Task<IActionResult> TrustDevice(Guid deviceId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _deviceIdentityService.TrustDeviceAsync(userId, deviceId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{deviceId:guid}/untrust")]
    public async Task<IActionResult> UntrustDevice(Guid deviceId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _deviceIdentityService.UntrustDeviceAsync(userId, deviceId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{deviceId:guid}/block")]
    public async Task<IActionResult> BlockDevice(Guid deviceId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _deviceIdentityService.BlockDeviceAsync(userId, deviceId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> RemoveDevice(Guid deviceId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _deviceIdentityService.RemoveDeviceAsync(userId, deviceId, cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID claim is missing or invalid.");
        }

        return userId;
    }
}
