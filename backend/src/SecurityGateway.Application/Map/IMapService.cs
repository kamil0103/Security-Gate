using SecurityGateway.Application.Map.DTOs;

namespace SecurityGateway.Application.Map;

public interface IMapService
{
    Task<IReadOnlyList<MapPointDto>> GetPointsAsync(MapFilterRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MapPointDto>> GetAttackPointsAsync(MapFilterRequest request, CancellationToken cancellationToken = default);
    Task<IpDetailsDto?> GetIpDetailsAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCountriesAsync(CancellationToken cancellationToken = default);
}
