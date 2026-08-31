using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl.Models;

public sealed class AccessEvaluationContext
{
    public Guid ApplicationId { get; init; }
    public string ClientIp { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public string? Browser { get; init; }
    public string? OperatingSystem { get; init; }
    public string? DeviceFingerprint { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceId { get; init; }
    public string? SessionId { get; init; }
    public Guid? UserId { get; init; }
    public string? Username { get; init; }
    public string HttpMethod { get; init; } = "GET";
    public string RequestedPath { get; init; } = "/";
    public string? QueryString { get; init; }
    public bool IsAuthenticated { get; init; }
    public string? CloudflareCountry { get; init; }
}

public sealed class AccessEvaluationResult
{
    public AccessEvaluationDecision Decision { get; init; }
    public string? Reason { get; init; }
    public AccessRequest? AccessRequest { get; init; }
    public string? PublicId { get; init; }
}

public enum AccessEvaluationDecision
{
    Allow,
    Challenge,
    Deny,
    Block
}
