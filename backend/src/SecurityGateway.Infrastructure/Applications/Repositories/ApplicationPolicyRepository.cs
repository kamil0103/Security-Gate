using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Applications;
using SecurityGateway.Domain.Applications;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.Applications.Repositories;

public sealed class ApplicationPolicyRepository : IApplicationPolicyRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationPolicyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ApplicationPolicy?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        return _context.ApplicationPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationId == applicationId, cancellationToken);
    }

    public async Task AddAsync(ApplicationPolicy policy, CancellationToken cancellationToken = default)
    {
        await _context.ApplicationPolicies.AddAsync(policy, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ApplicationPolicy policy, CancellationToken cancellationToken = default)
    {
        var tracked = _context.ApplicationPolicies.Local.FirstOrDefault(p => p.Id == policy.Id);

        if (tracked is not null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(policy);
        }
        else
        {
            _context.ApplicationPolicies.Update(policy);
        }

        return Task.CompletedTask;
    }
}
