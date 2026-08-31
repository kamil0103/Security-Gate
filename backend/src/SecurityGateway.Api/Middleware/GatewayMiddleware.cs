using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.Models;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Applications.DTOs;
using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Application.RateLimiting.Models;
using SecurityGateway.Domain.Audit;

namespace SecurityGateway.Api.Middleware;

public sealed class GatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IProxyService _proxyService;
    private readonly IClientIpResolver _clientIpResolver;
    private readonly IIpIntelligenceService? _ipIntelligenceService;
    private readonly IApplicationPolicyService _applicationPolicyService;
    private readonly IAccessControlService _accessControlService;
    private readonly IAccessRequestService _accessRequestService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAutomaticBlockingService _automaticBlockingService;
    private readonly IAuditService _auditService;
    private readonly GatewayOptions _options;
    private readonly ILogger<GatewayMiddleware> _logger;

    public GatewayMiddleware(
        RequestDelegate next,
        IProxyService proxyService,
        IClientIpResolver clientIpResolver,
        IIpIntelligenceService? ipIntelligenceService,
        IApplicationPolicyService applicationPolicyService,
        IAccessControlService accessControlService,
        IAccessRequestService accessRequestService,
        IRateLimitService rateLimitService,
        IAutomaticBlockingService automaticBlockingService,
        IAuditService auditService,
        GatewayOptions options,
        ILogger<GatewayMiddleware> logger)
    {
        _next = next;
        _proxyService = proxyService;
        _clientIpResolver = clientIpResolver;
        _ipIntelligenceService = ipIntelligenceService;
        _applicationPolicyService = applicationPolicyService;
        _accessControlService = accessControlService;
        _accessRequestService = accessRequestService;
        _rateLimitService = rateLimitService;
        _automaticBlockingService = automaticBlockingService;
        _auditService = auditService;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (IsAdminPath(path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var clientIpResult = _clientIpResolver.Resolve(BuildClientIpContext(context));
        var clientIp = clientIpResult.ClientIp;

        _logger.LogInformation(
            "Gateway request {Method} {Path}{QueryString} from {ClientIp} (trusted: {IsTrusted}, chain: {ProxyChain})",
            context.Request.Method,
            path,
            context.Request.QueryString.Value,
            clientIp,
            clientIpResult.IsTrusted,
            string.Join(" -> ", clientIpResult.ProxyChain));

        if (_ipIntelligenceService is not null)
        {
            try
            {
                await _ipIntelligenceService.TrackAsync(new TrackIpRequest
                {
                    IpAddress = clientIp
                }, context.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-critical IP intelligence tracking failed for {ClientIp}.", clientIp);
            }
        }

        var host = context.Request.Host.Host;
        var application = await _applicationPolicyService.GetApplicationByDomainAsync(host, context.RequestAborted).ConfigureAwait(false);

        if (application is not null && !application.IsEnabled)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Application is disabled.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        if (application is null)
        {
            await ProxyToUpstreamAsync(context, path, clientIp, null).ConfigureAwait(false);
            return;
        }

        var userId = GetUserId(context);
        var username = context.User.Identity?.Name;
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var sessionId = GetOrCreateSessionId(context);
        var fingerprint = ComputeFingerprint(userAgent, sessionId);
        var cloudflareCountry = context.Request.Headers.GetCommaSeparatedValues("CF-IPCountry").FirstOrDefault();

        var evaluation = await _accessRequestService.EvaluateAccessAsync(new AccessEvaluationContext
        {
            ApplicationId = application.Id,
            ClientIp = clientIp,
            UserAgent = userAgent,
            DeviceFingerprint = fingerprint,
            DeviceName = null,
            DeviceId = null,
            SessionId = sessionId,
            UserId = userId,
            Username = username,
            HttpMethod = context.Request.Method,
            RequestedPath = path,
            QueryString = context.Request.QueryString.Value,
            IsAuthenticated = isAuthenticated,
            CloudflareCountry = cloudflareCountry
        }, context.RequestAborted).ConfigureAwait(false);

        switch (evaluation.Decision)
        {
            case AccessEvaluationDecision.Allow:
                break;

            case AccessEvaluationDecision.Challenge:
                EnsureSessionCookie(context, sessionId);
                await RenderChallengePageAsync(context, evaluation.PublicId ?? evaluation.AccessRequest?.PublicId ?? "UNKNOWN").ConfigureAwait(false);
                return;

            case AccessEvaluationDecision.Deny:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(evaluation.Reason ?? "Access denied.", context.RequestAborted).ConfigureAwait(false);
                await AuditDecisionAsync("AccessDenied", application, clientIp, userId, username, evaluation.Reason).ConfigureAwait(false);
                return;

            case AccessEvaluationDecision.Block:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(evaluation.Reason ?? "Access blocked.", context.RequestAborted).ConfigureAwait(false);
                await AuditDecisionAsync("AccessBlocked", application, clientIp, userId, username, evaluation.Reason).ConfigureAwait(false);
                return;
        }

        var rateLimitContext = new RateLimitRequestContext
        {
            IpAddress = clientIp,
            UserId = userId,
            Domain = host,
            Endpoint = path
        };

        var rateLimitResult = await _rateLimitService.CheckAsync(rateLimitContext, context.RequestAborted).ConfigureAwait(false);

        if (!rateLimitResult.Allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = rateLimitResult.ResetAt.Subtract(DateTimeOffset.UtcNow).TotalSeconds.ToString("0");
            await context.Response.WriteAsync(rateLimitResult.Reason ?? "Rate limit exceeded.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await ProxyToUpstreamAsync(context, path, clientIp, application).ConfigureAwait(false);
    }

    private async Task ProxyToUpstreamAsync(HttpContext context, string path, string clientIp, ApplicationDto? application)
    {
        var upstreamUrl = application?.UpstreamUrl ?? _options.UpstreamNpmUrl;

        if (string.IsNullOrWhiteSpace(upstreamUrl))
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync("No upstream configured for the requested host.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        var proxyRequest = new ProxyRequestContext
        {
            Method = context.Request.Method,
            Path = path,
            QueryString = context.Request.QueryString.Value ?? string.Empty,
            Host = context.Request.Host.Host,
            Headers = context.Request.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.Where(v => v is not null).Cast<string>().AsEnumerable(),
                StringComparer.OrdinalIgnoreCase),
            Body = context.Request.Body,
            ClientIp = clientIp
        };

        using var proxyResponse = await _proxyService.ForwardAsync(proxyRequest, upstreamUrl, context.RequestAborted).ConfigureAwait(false);

        context.Response.StatusCode = proxyResponse.StatusCode;

        foreach (var (name, values) in proxyResponse.Headers)
        {
            context.Response.Headers[name] = values.ToArray();
        }

        await proxyResponse.Body.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task RenderChallengePageAsync(HttpContext context, string publicId)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";

        var host = context.Request.Host.Host;
        var returnPath = context.Request.Path.Value ?? "/";
        var query = context.Request.QueryString.Value;
        var continueUrl = returnPath + (query ?? string.Empty);

        var html = $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Security Gateway - Access Approval Required</title>
<style>
  body {{ font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }}
  .card {{ background: #1e293b; padding: 2rem; border-radius: 1rem; max-width: 480px; width: 90%; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.3); }}
  h1 {{ margin: 0 0 0.5rem; font-size: 1.5rem; color: #38bdf8; }}
  p {{ line-height: 1.6; color: #94a3b8; }}
  .request-id {{ font-family: monospace; background: #0f172a; padding: 0.5rem; border-radius: 0.5rem; color: #38bdf8; word-break: break-all; }}
  .status {{ font-weight: 700; color: #f59e0b; }}
  button {{ margin-top: 1rem; padding: 0.75rem 1.25rem; border: none; border-radius: 0.5rem; background: #38bdf8; color: #0f172a; font-weight: 700; cursor: pointer; }}
  button:disabled {{ opacity: 0.5; cursor: not-allowed; }}
</style>
</head>
<body>
<div class='card'>
  <h1>Security Gateway</h1>
  <p>Access approval is required for <strong>{System.Net.WebUtility.HtmlEncode(host)}</strong>.</p>
  <p>Request ID:</p>
  <p class='request-id'>{System.Net.WebUtility.HtmlEncode(publicId)}</p>
  <p>Status: <span id='status' class='status'>Waiting for administrator approval</span></p>
  <p id='message'>An administrator must approve this request before you can continue.</p>
  <button id='continue' disabled>Continue</button>
</div>
<script>
  const publicId = {System.Text.Json.JsonEncodedText.Encode(publicId)};
  const continueUrl = {System.Text.Json.JsonEncodedText.Encode(continueUrl)};
  const statusEl = document.getElementById('status');
  const messageEl = document.getElementById('message');
  const continueBtn = document.getElementById('continue');

  async function checkStatus() {{
    try {{
      const res = await fetch('/api/access-requests/' + encodeURIComponent(publicId) + '/status');
      if (!res.ok) return;
      const data = await res.json();
      statusEl.textContent = data.status;
      if (data.status === 'Approved') {{
        messageEl.textContent = 'Access approved. You may continue.';
        continueBtn.disabled = false;
        continueBtn.onclick = () => window.location.href = continueUrl;
      }} else if (data.status === 'Denied') {{
        messageEl.textContent = 'Access denied.' + (data.reason ? ' ' + data.reason : '');
      }} else if (data.status === 'Expired') {{
        messageEl.textContent = 'Request expired. Please refresh the page to request access again.';
      }}
    }} catch (e) {{}}
  }}

  setInterval(checkStatus, 3000);
  checkStatus();
</script>
</body>
</html>";

        await context.Response.WriteAsync(html, context.RequestAborted).ConfigureAwait(false);
    }

    private string GetOrCreateSessionId(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue("sg_session", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Guid.NewGuid().ToString("N");
    }

    private void EnsureSessionCookie(HttpContext context, string sessionId)
    {
        if (context.Request.Cookies.ContainsKey("sg_session"))
        {
            return;
        }

        var options = new CookieOptions
        {
            Domain = context.Request.Host.Host,
            Path = "/",
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        };

        context.Response.Cookies.Append("sg_session", sessionId, options);
    }

    private static string ComputeFingerprint(string userAgent, string sessionId)
    {
        var input = string.IsNullOrWhiteSpace(userAgent) ? sessionId : userAgent;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }

    private async Task AuditDecisionAsync(string action, ApplicationDto application, string clientIp, Guid? userId, string? username, string? reason)
    {
        try
        {
            await _auditService.LogAsync(
                AuditCategory.AccessControl,
                action,
                userId,
                username,
                clientIp,
                $"{action} for {application.Domain}. Reason: {reason}",
                false,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort audit.
        }
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private bool IsAdminPath(string path)
    {
        foreach (var prefix in _options.AdminPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static ClientIpContext BuildClientIpContext(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        var additionalHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CF-Connecting-IP"] = context.Request.Headers.GetCommaSeparatedValues("CF-Connecting-IP"),
            ["CF-Visitor-IP"] = context.Request.Headers.GetCommaSeparatedValues("CF-Visitor-IP"),
            ["CF-IPCountry"] = context.Request.Headers.GetCommaSeparatedValues("CF-IPCountry"),
            ["CF-Ray"] = context.Request.Headers.GetCommaSeparatedValues("CF-Ray")
        };

        return new ClientIpContext
        {
            RemoteIp = remoteIp,
            ForwardedFor = context.Request.Headers.GetCommaSeparatedValues("X-Forwarded-For"),
            RealIp = context.Request.Headers.GetCommaSeparatedValues("X-Real-IP"),
            Forwarded = context.Request.Headers.GetCommaSeparatedValues("Forwarded"),
            AdditionalHeaders = additionalHeaders
        };
    }
}

internal static class HeaderExtensions
{
    public static IReadOnlyList<string> GetCommaSeparatedValues(this IHeaderDictionary headers, string name)
    {
        if (!headers.TryGetValue(name, out var values))
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
