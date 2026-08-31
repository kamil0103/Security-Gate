using System.Text.RegularExpressions;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Application.Waf;
using SecurityGateway.Application.Waf.DTOs;
using SecurityGateway.Domain.Waf;

namespace SecurityGateway.Api.Middleware;

public sealed class InlineWafMiddleware
{
    private readonly RequestDelegate _next;
    private readonly InlineWafOptions _options;
    private readonly IClientIpResolver _clientIpResolver;
    private readonly IWafEventService _wafEventService;
    private readonly ILogger<InlineWafMiddleware> _logger;

    private static readonly IReadOnlyList<WafRule> Rules = new List<WafRule>
    {
        new("942100", AttackType.SqlInjection, AttackSeverity.High,
            @"(\b(union|select|insert|update|delete|drop|alter|create|exec|execute|sp_executesql)\b.*\b(from|into|table|database)\b)|(--\s)|(/\*\s*)|(\b(or|and)\b\s+\d+\s*=\s*\d+)"),
        new("941100", AttackType.CrossSiteScripting, AttackSeverity.High,
            @"(<\s*(script|iframe|object|embed|applet|form|svg|math)|javascript:|on\w+\s*=|\balert\s*\(|\bdocument\.cookie|\blocation\.href|\beval\s*\()"),
        new("930100", AttackType.PathTraversal, AttackSeverity.Medium,
            @"\.\./|\.\.\\|%2e%2e(/|\\)|%2e%2e%2f|\.{2,}[/\\]"),
        new("942200", AttackType.SqlInjection, AttackSeverity.Medium,
            @"(\bunion\b.*\bselect\b)|(\bselect\b.*\bfrom\b.*\bwhere\b)|(\b1\s*=\s*1\b)|(\bsleep\s*\()|(\bbenchmark\s*\()"),
    };

    public InlineWafMiddleware(
        RequestDelegate next,
        InlineWafOptions options,
        IClientIpResolver clientIpResolver,
        IWafEventService wafEventService,
        ILogger<InlineWafMiddleware> logger)
    {
        _next = next;
        _options = options;
        _clientIpResolver = clientIpResolver;
        _wafEventService = wafEventService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var request = context.Request;
        var input = $"{request.Method} {request.Path}{request.QueryString} {request.Headers.UserAgent}";

        foreach (var rule in Rules)
        {
            if (!rule.Regex.IsMatch(input))
            {
                continue;
            }

            var clientIp = ResolveClientIp(context);
            _logger.LogWarning(
                "Inline WAF rule {RuleId} matched for {ClientIp} on {Method} {Path}{QueryString}",
                rule.Id,
                clientIp,
                request.Method,
                request.Path,
                request.QueryString);

            var action = _options.LogOnly ? WafAction.Logged : WafAction.Blocked;

            try
            {
                await _wafEventService.IngestAsync(new CreateWafEventRequest
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    SourceIp = clientIp,
                    RequestId = context.TraceIdentifier,
                    RuleId = rule.Id,
                    RuleMessage = $"Inline WAF: {rule.AttackType}",
                    Severity = rule.Severity,
                    AttackType = rule.AttackType,
                    Method = request.Method,
                    Uri = $"{request.Path}{request.QueryString}",
                    Host = request.Host.Host,
                    Action = action,
                    RawLog = input
                }, context.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record inline WAF event.");
            }

            if (_options.LogOnly)
            {
                break;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Request blocked by WAF.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private string ResolveClientIp(HttpContext context)
    {
        try
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            var forwardedFor = context.Request.Headers.GetCommaSeparatedValues("X-Forwarded-For").ToList();
            var realIp = context.Request.Headers.GetCommaSeparatedValues("X-Real-Ip").ToList();

            var forwarded = context.Request.Headers.GetCommaSeparatedValues("Forwarded").ToList();
            var result = _clientIpResolver.Resolve(new ClientIpContext
            {
                RemoteIp = remoteIp,
                ForwardedFor = forwardedFor,
                RealIp = realIp,
                Forwarded = forwarded
            });
            return result.ClientIp;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve client IP for inline WAF.");
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    private sealed record WafRule(string Id, AttackType AttackType, AttackSeverity Severity, string Pattern)
    {
        public Regex Regex { get; } = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

public sealed class InlineWafOptions
{
    public const string SectionName = "InlineWaf";

    public bool Enabled { get; set; } = true;
    public bool LogOnly { get; set; } = false;
}
