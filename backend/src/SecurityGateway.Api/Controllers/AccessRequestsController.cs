using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.DTOs;
using System.Security.Claims;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRequestsController : ControllerBase
{
    private readonly IAccessRequestService _accessRequestService;

    public AccessRequestsController(IAccessRequestService accessRequestService)
    {
        _accessRequestService = accessRequestService;
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var pending = await _accessRequestService.GetPendingAsync(cancellationToken).ConfigureAwait(false);
        return Ok(pending);
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20, CancellationToken cancellationToken = default)
    {
        var recent = await _accessRequestService.GetRecentAsync(count, cancellationToken).ConfigureAwait(false);
        return Ok(recent);
    }

    [HttpGet("{publicId}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(string publicId, CancellationToken cancellationToken)
    {
        var status = await _accessRequestService.GetStatusAsync(publicId, cancellationToken).ConfigureAwait(false);
        return Ok(status);
    }

    [HttpGet("{publicId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetByPublicId(string publicId, CancellationToken cancellationToken)
    {
        var request = await _accessRequestService.GetByPublicIdAsync(publicId, cancellationToken).ConfigureAwait(false);
        return request is null ? NotFound() : Ok(request);
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveAccessRequestRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = GetCurrentUserId();
        var result = await _accessRequestService.ResolveAsync(id, adminUserId, request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
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
