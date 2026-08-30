namespace SecurityGateway.Application.IpIntelligence;

public interface IIpIntelligenceService
{
    Task<IpAddressDto> TrackAsync(TrackIpRequest request, CancellationToken cancellationToken = default);
    Task<IpAddressDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IpAddressDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
