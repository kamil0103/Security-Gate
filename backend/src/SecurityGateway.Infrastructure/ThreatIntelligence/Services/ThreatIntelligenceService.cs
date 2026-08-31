using SecurityGateway.Application.ThreatIntelligence;

namespace SecurityGateway.Infrastructure.ThreatIntelligence.Services;

public class ThreatIntelligenceService : IThreatIntelligenceService
{
    private readonly IEnumerable<IThreatIntelligenceProvider> _providers;

    public ThreatIntelligenceService(IEnumerable<IThreatIntelligenceProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<ThreatIntelligenceResult>> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var results = new List<ThreatIntelligenceResult>();

        foreach (var provider in _providers)
        {
            try
            {
                var result = await provider.LookupAsync(ipAddress, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }
            catch
            {
                // Providers are best-effort; failures should not break request handling.
            }
        }

        return results;
    }
}
