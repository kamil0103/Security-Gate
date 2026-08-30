using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Domain.AccessControl;
using System.Security.Claims;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/access-control")]
[Authorize(Roles = "Administrator")]
public class AccessControlController : ControllerBase
{
    private readonly IAccessControlService _accessControlService;

    public AccessControlController(IAccessControlService accessControlService)
    {
        _accessControlService = accessControlService;
    }

    [HttpGet("trusted-networks")]
    public async Task<IActionResult> GetTrustedNetworks(CancellationToken cancellationToken)
    {
        var networks = await _accessControlService.GetTrustedNetworksAsync(cancellationToken);
        return Ok(networks);
    }

    [HttpGet("trusted-networks/{id:guid}")]
    public async Task<IActionResult> GetTrustedNetworkById(Guid id, CancellationToken cancellationToken)
    {
        var network = await _accessControlService.GetTrustedNetworkByIdAsync(id, cancellationToken);
        return network is null ? NotFound() : Ok(network);
    }

    [HttpPost("trusted-networks")]
    public async Task<IActionResult> CreateTrustedNetwork([FromBody] CreateTrustedNetworkRequest request, CancellationToken cancellationToken)
    {
        var network = await _accessControlService.CreateTrustedNetworkAsync(request, cancellationToken);
        return Created($"/api/access-control/trusted-networks/{network.Id}", network);
    }

    [HttpPut("trusted-networks/{id:guid}")]
    public async Task<IActionResult> UpdateTrustedNetwork(Guid id, [FromBody] UpdateTrustedNetworkRequest request, CancellationToken cancellationToken)
    {
        var network = await _accessControlService.UpdateTrustedNetworkAsync(id, request, cancellationToken);
        return Ok(network);
    }

    [HttpDelete("trusted-networks/{id:guid}")]
    public async Task<IActionResult> DeleteTrustedNetwork(Guid id, CancellationToken cancellationToken)
    {
        await _accessControlService.DeleteTrustedNetworkAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("blocklist")]
    public async Task<IActionResult> GetBlocklist(CancellationToken cancellationToken)
    {
        var entries = await _accessControlService.GetBlocklistEntriesAsync(cancellationToken);
        return Ok(entries);
    }

    [HttpGet("blocklist/{id:guid}")]
    public async Task<IActionResult> GetBlocklistEntryById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _accessControlService.GetBlocklistEntryByIdAsync(id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost("blocklist")]
    public async Task<IActionResult> CreateBlocklistEntry([FromBody] CreateBlocklistEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await _accessControlService.CreateBlocklistEntryAsync(request, GetCurrentUserId(), cancellationToken);
        return Created($"/api/access-control/blocklist/{entry.Id}", entry);
    }

    [HttpPut("blocklist/{id:guid}")]
    public async Task<IActionResult> UpdateBlocklistEntry(Guid id, [FromBody] CreateBlocklistEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await _accessControlService.UpdateBlocklistEntryAsync(id, request, cancellationToken);
        return Ok(entry);
    }

    [HttpDelete("blocklist/{id:guid}")]
    public async Task<IActionResult> DeleteBlocklistEntry(Guid id, CancellationToken cancellationToken)
    {
        await _accessControlService.DeleteBlocklistEntryAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("devices/{deviceId:guid}/approve")]
    public async Task<IActionResult> ApproveDevice(Guid deviceId, [FromBody] AccessDecisionRequest? request, CancellationToken cancellationToken)
    {
        var decision = await _accessControlService.ApproveDeviceAsync(deviceId, GetCurrentUserId(), request?.Reason, cancellationToken);
        return Ok(decision);
    }

    [HttpPost("devices/{deviceId:guid}/deny")]
    public async Task<IActionResult> DenyDevice(Guid deviceId, [FromBody] AccessDecisionRequest? request, CancellationToken cancellationToken)
    {
        var decision = await _accessControlService.DenyDeviceAsync(deviceId, GetCurrentUserId(), request?.Reason, cancellationToken);
        return Ok(decision);
    }

    [HttpGet("devices/{deviceId:guid}/decisions")]
    public async Task<IActionResult> GetDeviceDecisions(Guid deviceId, CancellationToken cancellationToken)
    {
        var decisions = await _accessControlService.GetDecisionsForTargetAsync(AccessDecisionType.DeviceApproval, deviceId, cancellationToken);
        return Ok(decisions);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("Unable to determine current user ID.");
        }

        return userId;
    }
}
