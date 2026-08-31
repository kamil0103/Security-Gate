using SecurityGateway.Domain.Applications;

namespace SecurityGateway.Application.Applications;

public interface IApplicationPolicyRepository
{
    Task<ApplicationPolicy?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationPolicy policy, CancellationToken cancellationToken = default);
}
