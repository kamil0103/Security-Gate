using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Applications.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/applications/{applicationId:guid}/policy")]
[Authorize(Roles = "Administrator")]
public class ApplicationPoliciesController : ControllerBase
{
    private readonly IApplicationPolicyService _applicationPolicyService;

    public ApplicationPoliciesController(IApplicationPolicyService applicationPolicyService)
    {
        _applicationPolicyService = applicationPolicyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPolicy(Guid applicationId, CancellationToken cancellationToken)
    {
        var policy = await _applicationPolicyService.GetPolicyAsync(applicationId, cancellationToken);
        return policy is null ? NotFound() : Ok(policy);
    }

    [HttpPut]
    public async Task<IActionResult> UpdatePolicy(Guid applicationId, [FromBody] UpdateApplicationPolicyRequest request, CancellationToken cancellationToken)
    {
        var policy = await _applicationPolicyService.UpdatePolicyAsync(applicationId, request, cancellationToken);
        return Ok(policy);
    }
}
