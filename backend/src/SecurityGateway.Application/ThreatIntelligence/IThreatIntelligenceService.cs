namespace SecurityGateway.Application.ThreatIntelligence;

public interface IThreatIntelligenceService
{
    Task<IReadOnlyList<ThreatIntelligenceResult>> LookupAsync(string ipAddress, CancellationToken cancellationToken = default);
}
