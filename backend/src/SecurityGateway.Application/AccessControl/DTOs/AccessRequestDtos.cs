using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed class AccessRequestDto
{
    public Guid Id { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? ResolutionReason { get; init; }

    public Guid ApplicationId { get; init; }
    public string ApplicationName { get; init; } = string.Empty;
    public string ApplicationDomain { get; init; } = string.Empty;

    public string HttpMethod { get; init; } = string.Empty;
    public string RequestedPath { get; init; } = string.Empty;

    public string ClientIp { get; init; } = string.Empty;
    public string? Country { get; init; }
    public string? CountryCode { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public string? Isp { get; init; }
    public string? Asn { get; init; }

    public bool IsVpn { get; init; }
    public bool IsProxy { get; init; }
    public bool IsTor { get; init; }
    public bool IsDatacenter { get; init; }

    public int ThreatScore { get; init; }
    public string? ThreatLevel { get; init; }
    public int RequestCount { get; init; }

    public string? DeviceFingerprint { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceId { get; init; }
    public string? SessionId { get; init; }

    public string? UserAgent { get; init; }
    public string? Browser { get; init; }
    public string? OperatingSystem { get; init; }

    public Guid? UserId { get; init; }
    public string? Username { get; init; }

    public string ReasonForChallenge { get; init; } = string.Empty;

    public Guid? ReviewedByUserId { get; init; }
    public string? ReviewedByUsername { get; init; }
    public string? Decision { get; init; }
    public string? ApprovalScope { get; init; }
}

public sealed class AccessRequestStatusDto
{
    public string PublicId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public string? Reason { get; init; }
}

public sealed class ResolveAccessRequestRequest
{
    public required AccessRequestDecision Decision { get; init; }
    public ApprovalScope? ApprovalScope { get; init; }
    public string? Reason { get; init; }
}
