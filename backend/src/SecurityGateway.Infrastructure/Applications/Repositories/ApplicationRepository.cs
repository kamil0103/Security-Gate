using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Applications;
using SecurityGateway.Infrastructure.Persistence;
using ApplicationEntity = SecurityGateway.Domain.Applications.Application;

namespace SecurityGateway.Infrastructure.Applications.Repositories;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ApplicationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Applications
            .AsNoTracking()
            .Include(a => a.Policy)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<ApplicationEntity?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        return _context.Applications
            .AsNoTracking()
            .Include(a => a.Policy)
            .FirstOrDefaultAsync(a => a.Domain == domain, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _context.Applications
            .AsNoTracking()
            .Include(a => a.Policy)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return applications.AsReadOnly();
    }

    public async Task AddAsync(ApplicationEntity application, CancellationToken cancellationToken = default)
    {
        await _context.Applications.AddAsync(application, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ApplicationEntity application, CancellationToken cancellationToken = default)
    {
        var tracked = _context.Applications.Local.FirstOrDefault(a => a.Id == application.Id);

        if (tracked is not null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(application);
        }
        else
        {
            _context.Applications.Update(application);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(ApplicationEntity application, CancellationToken cancellationToken = default)
    {
        _context.Applications.Remove(application);
        return Task.CompletedTask;
    }
}
