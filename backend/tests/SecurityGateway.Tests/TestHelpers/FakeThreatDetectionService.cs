using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.ThreatDetection.DTOs;
using SecurityGateway.Application.ThreatDetection.Models;

namespace SecurityGateway.Tests.TestHelpers;

public sealed class FakeThreatDetectionService : IThreatDetectionService
{
    public List<CreateSecurityEventRequest> RecordedEvents { get; } = new();

    public Task<SecurityEventDto> RecordEventAsync(CreateSecurityEventRequest request, CancellationToken cancellationToken = default)
    {
        RecordedEvents.Add(request);

        return Task.FromResult(new SecurityEventDto
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Type = request.Type,
            Severity = request.Severity,
            SourceIp = request.SourceIp,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            Description = request.Description,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public Task<SecurityEventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<SecurityEventDto?>(null);

    public Task<IReadOnlyList<SecurityEventDto>> GetRecentEventsAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SecurityEventDto>>(Array.Empty<SecurityEventDto>());

    public Task<IReadOnlyList<SecurityEventDto>> SearchEventsAsync(SecurityEventFilter filter, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SecurityEventDto>>(Array.Empty<SecurityEventDto>());

    public Task<IReadOnlyList<ThreatScoreRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ThreatScoreRuleDto>>(Array.Empty<ThreatScoreRuleDto>());

    public Task<ThreatScoreRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<ThreatScoreRuleDto?>(null);

    public Task<ThreatScoreRuleDto> CreateRuleAsync(CreateThreatScoreRuleRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<ThreatScoreRuleDto> UpdateRuleAsync(Guid id, CreateThreatScoreRuleRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<ThreatScoreResult?> EvaluateThreatScoreAsync(string sourceIp, CancellationToken cancellationToken = default)
        => Task.FromResult<ThreatScoreResult?>(null);
}
