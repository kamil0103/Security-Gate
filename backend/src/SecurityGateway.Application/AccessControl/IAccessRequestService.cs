using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.AccessControl.Models;

namespace SecurityGateway.Application.AccessControl;

public interface IAccessRequestService
{
    Task<AccessEvaluationResult> EvaluateAccessAsync(AccessEvaluationContext context, CancellationToken cancellationToken = default);
    Task<AccessRequestDto?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    Task<AccessRequestStatusDto> GetStatusAsync(string publicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRequestDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<AccessRequestDto> ResolveAsync(Guid accessRequestId, Guid adminUserId, ResolveAccessRequestRequest request, CancellationToken cancellationToken = default);
    Task RevokeTrustAsync(Guid trustRecordId, Guid adminUserId, CancellationToken cancellationToken = default);
}
