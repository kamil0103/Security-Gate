using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.ThreatIntelligence;
using SecurityGateway.Domain.IpIntelligence;

namespace SecurityGateway.Infrastructure.IpIntelligence;

public sealed class IpIntelligenceService : IIpIntelligenceService
{
    private readonly IIpAddressRepository _ipAddressRepository;
    private readonly IGeoIpProvider _geoIpProvider;
    private readonly IReputationProvider _reputationProvider;
    private readonly IVpnProxyDetector _vpnProxyDetector;
    private readonly IThreatIntelligenceService _threatIntelligenceService;
    private readonly IUnitOfWork _unitOfWork;

    public IpIntelligenceService(
        IIpAddressRepository ipAddressRepository,
        IGeoIpProvider geoIpProvider,
        IReputationProvider reputationProvider,
        IVpnProxyDetector vpnProxyDetector,
        IThreatIntelligenceService threatIntelligenceService,
        IUnitOfWork unitOfWork)
    {
        _ipAddressRepository = ipAddressRepository;
        _geoIpProvider = geoIpProvider;
        _reputationProvider = reputationProvider;
        _vpnProxyDetector = vpnProxyDetector;
        _threatIntelligenceService = threatIntelligenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IpAddressDto> TrackAsync(TrackIpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IpAddress);

        var ip = await _ipAddressRepository.GetByIpAsync(request.IpAddress, cancellationToken).ConfigureAwait(false);

        if (ip is null)
        {
            ip = await CreateIpAddressAsync(request.IpAddress, cancellationToken).ConfigureAwait(false);
            await _ipAddressRepository.AddAsync(ip, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            ip.LastSeenAt = DateTimeOffset.UtcNow;
            ip.RequestCount++;
        }

        if (request.UserId.HasValue)
        {
            AssociateUser(ip, request.UserId.Value);
        }

        if (request.DeviceId.HasValue)
        {
            AssociateDevice(ip, request.DeviceId.Value);
        }

        await _ipAddressRepository.UpdateAsync(ip, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(ip);
    }

    public async Task<IpAddressDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ip = await _ipAddressRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ip is null ? null : MapToDto(ip);
    }

    public async Task<IReadOnlyList<IpAddressDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var ips = await _ipAddressRepository.GetRecentAsync(count, cancellationToken).ConfigureAwait(false);
        return ips.Select(MapToDto).ToList().AsReadOnly();
    }

    private async Task<IpAddress> CreateIpAddressAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var geoIpTask = _geoIpProvider.IsConfigured
            ? _geoIpProvider.LookupAsync(ipAddress, cancellationToken)
            : Task.FromResult(new GeoIpResult());

        var reputationTask = _reputationProvider.IsConfigured
            ? _reputationProvider.CheckAsync(ipAddress, cancellationToken)
            : Task.FromResult(new ReputationResult { Score = 0, ThreatLevel = "unknown", Source = "None" });

        var vpnProxyTask = _vpnProxyDetector.IsConfigured
            ? _vpnProxyDetector.CheckAsync(ipAddress, cancellationToken)
            : Task.FromResult(new VpnProxyResult { Source = "None" });

        var threatIntelTask = _threatIntelligenceService.LookupAsync(ipAddress, cancellationToken);

        await Task.WhenAll(geoIpTask, reputationTask, vpnProxyTask, threatIntelTask).ConfigureAwait(false);

        var geoIp = await geoIpTask;
        var reputation = await reputationTask;
        var vpnProxy = await vpnProxyTask;
        var threatIntel = await threatIntelTask;

        var externalScore = threatIntel.Any(r => r.IsMalicious)
            ? threatIntel.Where(r => r.IsMalicious).Max(r => r.ConfidenceScore)
            : 0;

        var finalScore = Math.Max(reputation.Score, externalScore);
        var threatLevel = finalScore switch
        {
            >= 80 => "critical",
            >= 60 => "high",
            >= 40 => "medium",
            >= 20 => "low",
            _ => reputation.ThreatLevel ?? "info"
        };

        return new IpAddress
        {
            Ip = ipAddress,
            CountryCode = geoIp.CountryCode,
            Country = geoIp.Country,
            Region = geoIp.Region,
            City = geoIp.City,
            Latitude = geoIp.Latitude,
            Longitude = geoIp.Longitude,
            Isp = geoIp.Isp,
            Organization = geoIp.Organization,
            Asn = geoIp.Asn,
            IsVpn = vpnProxy.IsVpn,
            IsProxy = vpnProxy.IsProxy,
            IsTor = vpnProxy.IsTor,
            IsDatacenter = vpnProxy.IsDatacenter,
            ThreatScore = finalScore,
            ThreatLevel = threatLevel,
            ReputationSource = threatIntel.Any(r => r.IsMalicious)
                ? $"{reputation.Source}; {string.Join(", ", threatIntel.Where(r => r.IsMalicious).Select(r => r.Source))}"
                : reputation.Source,
            RequestCount = 1
        };
    }

    private static void AssociateUser(IpAddress ip, Guid userId)
    {
        var association = ip.UserAssociations.FirstOrDefault(a => a.UserId == userId);

        if (association is not null)
        {
            association.LastSeenAt = DateTimeOffset.UtcNow;
            association.RequestCount++;
        }
        else
        {
            ip.UserAssociations.Add(new IpUserAssociation
            {
                IpAddressId = ip.Id,
                UserId = userId
            });
        }
    }

    private static void AssociateDevice(IpAddress ip, Guid deviceId)
    {
        var association = ip.DeviceAssociations.FirstOrDefault(a => a.DeviceId == deviceId);

        if (association is not null)
        {
            association.LastSeenAt = DateTimeOffset.UtcNow;
            association.RequestCount++;
        }
        else
        {
            ip.DeviceAssociations.Add(new IpDeviceAssociation
            {
                IpAddressId = ip.Id,
                DeviceId = deviceId
            });
        }
    }

    private static IpAddressDto MapToDto(IpAddress ip)
    {
        return new IpAddressDto
        {
            Id = ip.Id,
            Ip = ip.Ip,
            CountryCode = ip.CountryCode,
            Country = ip.Country,
            Region = ip.Region,
            City = ip.City,
            Latitude = ip.Latitude,
            Longitude = ip.Longitude,
            Isp = ip.Isp,
            Asn = ip.Asn,
            IsVpn = ip.IsVpn,
            IsProxy = ip.IsProxy,
            IsTor = ip.IsTor,
            IsDatacenter = ip.IsDatacenter,
            ThreatScore = ip.ThreatScore,
            ThreatLevel = ip.ThreatLevel,
            RequestCount = ip.RequestCount,
            AttackCount = ip.AttackCount,
            BlockCount = ip.BlockCount,
            FirstSeenAt = ip.FirstSeenAt,
            LastSeenAt = ip.LastSeenAt
        };
    }
}
