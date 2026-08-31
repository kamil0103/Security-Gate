using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("channels")]
    public async Task<IActionResult> GetChannels(CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetChannelsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("channels/{id:guid}")]
    public async Task<IActionResult> GetChannelById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetChannelByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("channels")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateNotificationChannelRequest request, CancellationToken cancellationToken)
    {
        var result = await _notificationService.CreateChannelAsync(request, cancellationToken);
        return Created($"/api/notifications/channels/{result.Id}", result);
    }

    [HttpPut("channels/{id:guid}")]
    public async Task<IActionResult> UpdateChannel(Guid id, [FromBody] CreateNotificationChannelRequest request, CancellationToken cancellationToken)
    {
        var result = await _notificationService.UpdateChannelAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("channels/{id:guid}")]
    public async Task<IActionResult> DeleteChannel(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.DeleteChannelAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("channels/{id:guid}/test")]
    public async Task<IActionResult> SendTest(Guid id, [FromBody] SendTestNotificationRequest request, CancellationToken cancellationToken)
    {
        await _notificationService.SendTestAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetRecentLogsAsync(limit, cancellationToken);
        return Ok(result);
    }
}
