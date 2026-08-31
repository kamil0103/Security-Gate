using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.Gateway;
using System.Security.Claims;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/access-requests")]
public class AccessRequestsController : ControllerBase
{
    private readonly IAccessRequestService _accessRequestService;
    private readonly IAccessControlService _accessControlService;
    private readonly IClientIpResolver _clientIpResolver;

    public AccessRequestsController(
        IAccessRequestService accessRequestService,
        IAccessControlService accessControlService,
        IClientIpResolver clientIpResolver)
    {
        _accessRequestService = accessRequestService;
        _accessControlService = accessControlService;
        _clientIpResolver = clientIpResolver;
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        if (!await IsTrustedAdminContextAsync(cancellationToken).ConfigureAwait(false))
        {
            return Forbid();
        }

        var pending = await _accessRequestService.GetPendingAsync(cancellationToken).ConfigureAwait(false);
        return Ok(pending);
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20, CancellationToken cancellationToken = default)
    {
        if (!await IsTrustedAdminContextAsync(cancellationToken).ConfigureAwait(false))
        {
            return Forbid();
        }

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
        if (!await IsTrustedAdminContextAsync(cancellationToken).ConfigureAwait(false))
        {
            return Forbid();
        }

        var request = await _accessRequestService.GetByPublicIdAsync(publicId, cancellationToken).ConfigureAwait(false);
        return request is null ? NotFound() : Ok(request);
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveAccessRequestRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTrustedAdminContextAsync(cancellationToken).ConfigureAwait(false))
        {
            return Forbid();
        }

        var adminUserId = GetCurrentUserId();
        var result = await _accessRequestService.ResolveAsync(id, adminUserId, request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    private async Task<bool> IsTrustedAdminContextAsync(CancellationToken cancellationToken)
    {
        var networks = await _accessControlService.GetTrustedNetworksAsync(cancellationToken).ConfigureAwait(false);

        // If no trusted networks are configured, allow initial setup.
        // Once at least one trusted network exists, admin actions must originate from it.
        if (networks.Count == 0)
        {
            return true;
        }

        var clientIp = _clientIpResolver.Resolve(BuildClientIpContext()).ClientIp;
        return await _accessControlService.IsIpTrustedAsync(clientIp, cancellationToken).ConfigureAwait(false);
    }

    private ClientIpContext BuildClientIpContext()
    {
        return new ClientIpContext
        {
            RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ForwardedFor = GetHeaderValues("X-Forwarded-For"),
            RealIp = GetHeaderValues("X-Real-IP"),
            Forwarded = GetHeaderValues("Forwarded"),
            AdditionalHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CF-Connecting-IP"] = GetHeaderValues("CF-Connecting-IP"),
                ["CF-Visitor-IP"] = GetHeaderValues("CF-Visitor-IP"),
                ["CF-IPCountry"] = GetHeaderValues("CF-IPCountry"),
                ["CF-Ray"] = GetHeaderValues("CF-Ray")
            }
        };
    }

    private IReadOnlyList<string> GetHeaderValues(string name)
    {
        if (!HttpContext.Request.Headers.TryGetValue(name, out var values))
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        foreach (var value in values)
        {
            var text = value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (var part in text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(part);
            }
        }

        return result.AsReadOnly();
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
