using SecurityGateway.Application.Applications.DTOs;
using SecurityGateway.Application.Applications.Models;

namespace SecurityGateway.Application.Applications;

public interface IApplicationPolicyService
{
    Task<IReadOnlyList<ApplicationDto>> GetApplicationsAsync(CancellationToken cancellationToken = default);
    Task<ApplicationDto?> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApplicationDto?> GetApplicationByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<ApplicationDto> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken cancellationToken = default);
    Task<ApplicationDto> UpdateApplicationAsync(Guid id, UpdateApplicationRequest request, CancellationToken cancellationToken = default);
    Task DeleteApplicationAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApplicationPolicyDto?> GetPolicyAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationPolicyDto> UpdatePolicyAsync(Guid applicationId, UpdateApplicationPolicyRequest request, CancellationToken cancellationToken = default);

    Task<ApplicationPolicyEvaluation> EvaluatePolicyAsync(Guid applicationId, string ipAddress, bool isAuthenticated, bool isIpTrusted, CancellationToken cancellationToken = default);
}
