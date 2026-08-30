using SecurityGateway.Application.Waf.DTOs;

namespace SecurityGateway.Application.Waf;

public interface IWafEventService
{
    Task<WafEventDto> IngestAsync(CreateWafEventRequest request, CancellationToken cancellationToken = default);
    Task<WafEventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WafEventDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WafEventDto>> SearchAsync(WafEventFilter filter, CancellationToken cancellationToken = default);
}
