using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.WebAuthn;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WebAuthnController : ControllerBase
{
    private readonly IWebAuthnService _webAuthnService;

    public WebAuthnController(IWebAuthnService webAuthnService)
    {
        _webAuthnService = webAuthnService;
    }

    [HttpPost("register/begin")]
    public async Task<IActionResult> BeginRegistration(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? userId.ToString();

        var result = await _webAuthnService.BeginRegistrationAsync(userId, username, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register/complete")]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _webAuthnService.CompleteRegistrationAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("credentials")]
    public async Task<IActionResult> GetCredentials(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var credentials = await _webAuthnService.GetCredentialsAsync(userId, cancellationToken);
        return Ok(credentials);
    }

    [HttpDelete("credentials/{credentialId}")]
    public async Task<IActionResult> DeleteCredential(string credentialId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _webAuthnService.DeleteCredentialAsync(userId, credentialId, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(value, out var userId) ? userId : throw new InvalidOperationException("User ID not found.");
    }
}
