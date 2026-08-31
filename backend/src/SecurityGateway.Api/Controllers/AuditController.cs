using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Audit.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] AuditLogFilterRequest filter, CancellationToken cancellationToken)
    {
        var logs = await _auditService.SearchAsync(filter, cancellationToken);
        var total = await _auditService.CountAsync(filter, cancellationToken);

        return Ok(new { total, skip = filter.Skip, take = filter.Take, logs });
    }
}
