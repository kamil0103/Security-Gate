using SecurityGateway.Domain.Applications;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.IpIntelligence;

namespace SecurityGateway.Domain.AccessControl;

public sealed class AccessRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PublicId { get; init; } = GeneratePublicId();
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionReason { get; set; }

    public Guid ApplicationId { get; init; }
    public Application Application { get; init; } = null!;
    public Guid? ApplicationPolicyId { get; init; }

    public string HttpMethod { get; init; } = "GET";
    public string RequestedPath { get; init; } = "/";
    public string? QueryString { get; init; }

    public string ClientIp { get; init; } = string.Empty;
    public Guid? IpAddressId { get; init; }
    public IpAddress? IpAddress { get; init; }

    public Guid? UserId { get; init; }
    public User? User { get; init; }
    public string? Username { get; init; }

    public string? DeviceFingerprint { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceId { get; init; }
    public string? SessionId { get; init; }

    public string? UserAgent { get; init; }
    public string? Browser { get; init; }
    public string? OperatingSystem { get; init; }

    public string? Country { get; init; }
    public string? CountryCode { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public string? Asn { get; init; }
    public string? Isp { get; init; }

    public bool IsVpn { get; init; }
    public bool IsProxy { get; init; }
    public bool IsTor { get; init; }
    public bool IsDatacenter { get; init; }

    public int ThreatScore { get; init; }
    public string? ThreatLevel { get; init; }
    public int RequestCount { get; set; } = 1;

    public string ReasonForChallenge { get; init; } = string.Empty;

    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public AccessRequestDecision? Decision { get; set; }
    public ApprovalScope? ApprovalScope { get; set; }

    public ICollection<TrustRecord> TrustRecords { get; init; } = new List<TrustRecord>();

    private static string GeneratePublicId()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[10];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var chars = new char[10];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }
        return new string(chars);
    }
}
