using SecurityGateway.Application.IpIntelligence;

namespace SecurityGateway.Infrastructure.IpIntelligence.Providers;

public sealed class NullReputationProvider : IReputationProvider
{
    public string Name => "None";
    public bool IsConfigured => false;

    public Task<ReputationResult> CheckAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ReputationResult
        {
            Score = 0,
            ThreatLevel = "unknown",
            Source = Name
        });
    }
}
