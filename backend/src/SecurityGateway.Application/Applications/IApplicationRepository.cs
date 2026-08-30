using ApplicationEntity = SecurityGateway.Domain.Applications.Application;

namespace SecurityGateway.Application.Applications;

public interface IApplicationRepository
{
    Task<ApplicationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApplicationEntity?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationEntity application, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationEntity application, CancellationToken cancellationToken = default);
    Task DeleteAsync(ApplicationEntity application, CancellationToken cancellationToken = default);
}
