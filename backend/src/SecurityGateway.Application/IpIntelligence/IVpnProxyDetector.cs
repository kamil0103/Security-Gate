namespace SecurityGateway.Application.IpIntelligence;

public interface IVpnProxyDetector
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<VpnProxyResult> CheckAsync(string ipAddress, CancellationToken cancellationToken = default);
}
