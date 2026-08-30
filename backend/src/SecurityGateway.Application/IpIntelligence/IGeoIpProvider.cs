namespace SecurityGateway.Application.IpIntelligence;

public interface IGeoIpProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<GeoIpResult> LookupAsync(string ipAddress, CancellationToken cancellationToken = default);
}
