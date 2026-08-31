namespace SecurityGateway.Application.ThreatIntelligence;

public interface IThreatIntelligenceProvider
{
    string Name { get; }
    Task<ThreatIntelligenceResult> LookupAsync(string ipAddress, CancellationToken cancellationToken = default);
}
