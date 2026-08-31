using SecurityGateway.Application.ThreatIntelligence;

namespace SecurityGateway.Tests.TestHelpers;

public sealed class FakeThreatIntelligenceService : IThreatIntelligenceService
{
    public Task<IReadOnlyList<ThreatIntelligenceResult>> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ThreatIntelligenceResult>>(new List<ThreatIntelligenceResult>());
}
