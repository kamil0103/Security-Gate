namespace SecurityGateway.Domain.IpIntelligence;

public sealed class IpAddress
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Ip { get; init; }

    public string? CountryCode { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Isp { get; set; }
    public string? Organization { get; set; }
    public string? Asn { get; set; }

    public bool IsVpn { get; set; }
    public bool IsProxy { get; set; }
    public bool IsTor { get; set; }
    public bool IsDatacenter { get; set; }

    public int ThreatScore { get; set; }
    public string? ThreatLevel { get; set; }
    public string? ReputationSource { get; set; }

    public long RequestCount { get; set; }
    public long AttackCount { get; set; }
    public long BlockCount { get; set; }

    public DateTimeOffset FirstSeenAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<IpUserAssociation> UserAssociations { get; init; } = new List<IpUserAssociation>();
    public ICollection<IpDeviceAssociation> DeviceAssociations { get; init; } = new List<IpDeviceAssociation>();
}
