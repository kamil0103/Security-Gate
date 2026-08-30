using SecurityGateway.Application.IpIntelligence;

namespace SecurityGateway.Infrastructure.IpIntelligence.Providers;

public sealed class NullVpnProxyDetector : IVpnProxyDetector
{
    public string Name => "None";
    public bool IsConfigured => false;

    public Task<VpnProxyResult> CheckAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new VpnProxyResult
        {
            Source = Name
        });
    }
}
