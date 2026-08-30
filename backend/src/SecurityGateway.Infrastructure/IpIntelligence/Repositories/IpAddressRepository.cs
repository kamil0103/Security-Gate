using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Domain.IpIntelligence;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.IpIntelligence.Repositories;

public sealed class IpAddressRepository : IIpAddressRepository
{
    private readonly ApplicationDbContext _context;

    public IpAddressRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IpAddress?> GetByIpAsync(string ip, CancellationToken cancellationToken = default)
    {
        return _context.IpAddresses
            .AsNoTracking()
            .Include(ip => ip.UserAssociations)
            .Include(ip => ip.DeviceAssociations)
            .FirstOrDefaultAsync(i => i.Ip == ip, cancellationToken);
    }

    public Task<IpAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.IpAddresses
            .AsNoTracking()
            .Include(ip => ip.UserAssociations)
            .Include(ip => ip.DeviceAssociations)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<IpAddress>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var ips = await _context.IpAddresses
            .AsNoTracking()
            .OrderByDescending(i => i.LastSeenAt)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ips.AsReadOnly();
    }

    public async Task AddAsync(IpAddress ipAddress, CancellationToken cancellationToken = default)
    {
        await _context.IpAddresses.AddAsync(ipAddress, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(IpAddress ipAddress, CancellationToken cancellationToken = default)
    {
        var tracked = _context.IpAddresses.Local.FirstOrDefault(i => i.Id == ipAddress.Id);

        if (tracked is not null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(ipAddress);
        }
        else
        {
            _context.IpAddresses.Update(ipAddress);
        }

        return Task.CompletedTask;
    }
}
