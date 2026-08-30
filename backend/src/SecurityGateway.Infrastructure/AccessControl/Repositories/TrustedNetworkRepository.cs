using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.AccessControl.Repositories;

public sealed class TrustedNetworkRepository : ITrustedNetworkRepository
{
    private readonly ApplicationDbContext _context;

    public TrustedNetworkRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<TrustedNetwork?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.TrustedNetworks
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public Task<TrustedNetwork?> GetByCidrAsync(string cidr, CancellationToken cancellationToken = default)
    {
        return _context.TrustedNetworks
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Cidr == cidr, cancellationToken);
    }

    public async Task<IReadOnlyList<TrustedNetwork>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var networks = await _context.TrustedNetworks
            .AsNoTracking()
            .OrderBy(n => n.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return networks.AsReadOnly();
    }

    public async Task<IReadOnlyList<TrustedNetwork>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        var networks = await _context.TrustedNetworks
            .AsNoTracking()
            .Where(n => n.IsEnabled)
            .OrderBy(n => n.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return networks.AsReadOnly();
    }

    public async Task AddAsync(TrustedNetwork network, CancellationToken cancellationToken = default)
    {
        await _context.TrustedNetworks.AddAsync(network, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(TrustedNetwork network, CancellationToken cancellationToken = default)
    {
        _context.TrustedNetworks.Update(network);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TrustedNetwork network, CancellationToken cancellationToken = default)
    {
        _context.TrustedNetworks.Remove(network);
        return Task.CompletedTask;
    }
}
