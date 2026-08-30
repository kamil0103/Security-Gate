using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly ApplicationDbContext _context;

    public DeviceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Devices
            .AsNoTracking()
            .Include(d => d.IpHistory)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public Task<Device?> GetByUserAndFingerprintAsync(Guid userId, string fingerprint, CancellationToken cancellationToken = default)
    {
        return _context.Devices
            .AsNoTracking()
            .Include(d => d.IpHistory)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Fingerprint == fingerprint, cancellationToken);
    }

    public Task<Device?> GetByUserAndDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
    {
        return _context.Devices
            .AsNoTracking()
            .Include(d => d.IpHistory)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.CredentialId == deviceId, cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await _context.Devices
            .AsNoTracking()
            .Include(d => d.IpHistory)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return devices.AsReadOnly();
    }

    public async Task<IReadOnlyList<Device>> GetPendingByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await _context.Devices
            .AsNoTracking()
            .Include(d => d.IpHistory)
            .Where(d => d.UserId == userId && d.TrustStatus == DeviceTrustStatus.Pending)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return devices.AsReadOnly();
    }

    public Task<bool> ExistsForUserAsync(Guid userId, string fingerprint, CancellationToken cancellationToken = default)
    {
        return _context.Devices.AnyAsync(d => d.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        await _context.Devices.AddAsync(device, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(Device device, CancellationToken cancellationToken = default)
    {
        var tracked = _context.Devices.Local.FirstOrDefault(d => d.Id == device.Id);

        if (tracked is not null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(device);
        }
        else
        {
            _context.Devices.Update(device);
        }

        return Task.CompletedTask;
    }
}
