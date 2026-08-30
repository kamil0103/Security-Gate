using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Applications.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationPolicyService _applicationPolicyService;

    public ApplicationsController(IApplicationPolicyService applicationPolicyService)
    {
        _applicationPolicyService = applicationPolicyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var applications = await _applicationPolicyService.GetApplicationsAsync(cancellationToken);
        return Ok(applications);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var application = await _applicationPolicyService.GetApplicationByIdAsync(id, cancellationToken);
        return application is null ? NotFound() : Ok(application);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request, CancellationToken cancellationToken)
    {
        var application = await _applicationPolicyService.CreateApplicationAsync(request, cancellationToken);
        return Created($"/api/applications/{application.Id}", application);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken cancellationToken)
    {
        var application = await _applicationPolicyService.UpdateApplicationAsync(id, request, cancellationToken);
        return Ok(application);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _applicationPolicyService.DeleteApplicationAsync(id, cancellationToken);
        return NoContent();
    }
}
