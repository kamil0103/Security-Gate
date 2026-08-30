using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl;

public interface ITrustedNetworkRepository
{
    Task<TrustedNetwork?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TrustedNetwork?> GetByCidrAsync(string cidr, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrustedNetwork>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrustedNetwork>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TrustedNetwork network, CancellationToken cancellationToken = default);
    Task UpdateAsync(TrustedNetwork network, CancellationToken cancellationToken = default);
    Task DeleteAsync(TrustedNetwork network, CancellationToken cancellationToken = default);
}
