using SecurityGateway.Domain.IpIntelligence;

namespace SecurityGateway.Application.IpIntelligence;

public interface IIpAddressRepository
{
    Task<IpAddress?> GetByIpAsync(string ip, CancellationToken cancellationToken = default);
    Task<IpAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IpAddress>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task AddAsync(IpAddress ipAddress, CancellationToken cancellationToken = default);
    Task UpdateAsync(IpAddress ipAddress, CancellationToken cancellationToken = default);
}
