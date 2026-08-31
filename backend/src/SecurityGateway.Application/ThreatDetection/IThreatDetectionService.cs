using SecurityGateway.Application.ThreatDetection.DTOs;
using SecurityGateway.Application.ThreatDetection.Models;

namespace SecurityGateway.Application.ThreatDetection;

public interface IThreatDetectionService
{
    Task<SecurityEventDto> RecordEventAsync(CreateSecurityEventRequest request, CancellationToken cancellationToken = default);
    Task<SecurityEventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEventDto>> GetRecentEventsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEventDto>> SearchEventsAsync(SecurityEventFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ThreatScoreRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task<ThreatScoreRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ThreatScoreRuleDto> CreateRuleAsync(CreateThreatScoreRuleRequest request, CancellationToken cancellationToken = default);
    Task<ThreatScoreRuleDto> UpdateRuleAsync(Guid id, CreateThreatScoreRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ThreatScoreResult?> EvaluateThreatScoreAsync(string sourceIp, CancellationToken cancellationToken = default);
}
