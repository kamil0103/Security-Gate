using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Map;
using SecurityGateway.Application.Map.DTOs;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Map.Services;

public class MapService : IMapService
{
    private readonly ApplicationDbContext _context;

    public MapService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MapPointDto>> GetPointsAsync(MapFilterRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.IpAddresses.AsQueryable();

        query = query.Where(ip => ip.Latitude.HasValue && ip.Longitude.HasValue);

        if (request.From.HasValue)
        {
            query = query.Where(ip => ip.LastSeenAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(ip => ip.LastSeenAt <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            query = query.Where(ip => ip.CountryCode == request.CountryCode);
        }

        if (request.MinThreatScore.HasValue)
        {
            query = query.Where(ip => ip.ThreatScore >= request.MinThreatScore.Value);
        }

        if (request.HasAttacks == true)
        {
            query = query.Where(ip => ip.AttackCount > 0);
        }

        if (request.IsBlocked == true)
        {
            query = query.Where(ip => ip.BlockCount > 0);
        }

        var points = await query
            .OrderByDescending(ip => ip.ThreatScore)
            .Take(request.Limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return points.Select(MapPoint).ToList();
    }

    public async Task<IReadOnlyList<MapPointDto>> GetAttackPointsAsync(MapFilterRequest request, CancellationToken cancellationToken = default)
    {
        var attackRequest = new MapFilterRequest
        {
            From = request.From,
            To = request.To,
            CountryCode = request.CountryCode,
            MinThreatScore = request.MinThreatScore,
            HasAttacks = true,
            IsBlocked = request.IsBlocked,
            Limit = request.Limit
        };

        return await GetPointsAsync(attackRequest, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IpDetailsDto?> GetIpDetailsAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var ip = await _context.IpAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Ip == ipAddress, cancellationToken)
            .ConfigureAwait(false);

        if (ip is null)
        {
            return null;
        }

        return new IpDetailsDto
        {
            IpAddress = ip.Ip,
            Country = ip.Country,
            CountryCode = ip.CountryCode,
            Region = ip.Region,
            City = ip.City,
            Latitude = ip.Latitude,
            Longitude = ip.Longitude,
            Isp = ip.Isp,
            Organization = ip.Organization,
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
            FirstSeenAt = ip.FirstSeenAt.DateTime,
            LastSeenAt = ip.LastSeenAt.DateTime
        };
    }

    public async Task<IReadOnlyList<string>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var countries = await _context.IpAddresses
            .Where(ip => !string.IsNullOrEmpty(ip.Country))
            .Select(ip => ip.Country!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return countries;
    }

    private static MapPointDto MapPoint(Domain.IpIntelligence.IpAddress ip)
    {
        return new MapPointDto
        {
            IpAddress = ip.Ip,
            Latitude = ip.Latitude!.Value,
            Longitude = ip.Longitude!.Value,
            Country = ip.Country,
            CountryCode = ip.CountryCode,
            City = ip.City,
            ThreatScore = ip.ThreatScore,
            RequestCount = ip.RequestCount,
            AttackCount = ip.AttackCount,
            LastSeenAt = ip.LastSeenAt.DateTime
        };
    }
}
