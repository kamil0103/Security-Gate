namespace SecurityGateway.Application.IpIntelligence;

public interface IReputationProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<ReputationResult> CheckAsync(string ipAddress, CancellationToken cancellationToken = default);
}
