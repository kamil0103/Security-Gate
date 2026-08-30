using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;

namespace SecurityGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IClientIpResolver _clientIpResolver;

    public AuthController(IAuthenticationService authenticationService, IClientIpResolver clientIpResolver)
    {
        _authenticationService = authenticationService;
        _clientIpResolver = clientIpResolver;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterWithDeviceRequest request, CancellationToken cancellationToken)
    {
        var (ip, userAgent) = GetRequestMetadata();
        var deviceRequest = EnrichDeviceRequest(request.Device, userAgent);
        var result = await _authenticationService.RegisterAsync(request.User, deviceRequest, ip, userAgent, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginWithDeviceRequest request, CancellationToken cancellationToken)
    {
        var (ip, userAgent) = GetRequestMetadata();
        var deviceRequest = EnrichDeviceRequest(request.Device, userAgent);
        var result = await _authenticationService.LoginAsync(request.User, deviceRequest, ip, userAgent, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authenticationService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.RefreshTokenAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _authenticationService.ChangePasswordAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authenticationService.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authenticationService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await _authenticationService.VerifyEmailAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _authenticationService.GetUserByIdAsync(userId, cancellationToken);

        return user is null ? NotFound() : Ok(user);
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

    private (string Ip, string UserAgent) GetRequestMetadata()
    {
        var clientIpResult = _clientIpResolver.Resolve(BuildClientIpContext());
        var userAgent = Request.Headers.UserAgent.ToString();
        return (clientIpResult.ClientIp, userAgent);
    }

    private DeviceEnrollmentRequest EnrichDeviceRequest(DeviceEnrollmentRequest? request, string userAgent)
    {
        if (request is null)
        {
            return new DeviceEnrollmentRequest
            {
                DeviceId = Guid.NewGuid().ToString(),
                Name = "Unknown Device",
                Fingerprint = HashUserAgent(userAgent),
                UserAgent = userAgent
            };
        }

        return request with
        {
            UserAgent = request.UserAgent ?? userAgent
        };
    }

    private static string HashUserAgent(string userAgent)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(userAgent));
        return Convert.ToHexString(bytes);
    }

    private ClientIpContext BuildClientIpContext()
    {
        return new ClientIpContext
        {
            RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ForwardedFor = GetHeaderValues("X-Forwarded-For"),
            RealIp = GetHeaderValues("X-Real-IP"),
            Forwarded = GetHeaderValues("Forwarded")
        };
    }

    private IReadOnlyList<string> GetHeaderValues(string name)
    {
        if (!Request.Headers.TryGetValue(name, out var values))
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
}
