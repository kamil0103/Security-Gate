using SecurityGateway.Application.IpIntelligence;

namespace SecurityGateway.Infrastructure.IpIntelligence.Providers;

public sealed class NullGeoIpProvider : IGeoIpProvider
{
    public string Name => "None";
    public bool IsConfigured => false;

    public Task<GeoIpResult> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GeoIpResult());
    }
}
