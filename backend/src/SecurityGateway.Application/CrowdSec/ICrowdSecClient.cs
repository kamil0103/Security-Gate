namespace SecurityGateway.Application.CrowdSec;

public interface ICrowdSecClient
{
    Task<bool> IsIpMaliciousAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task ReportIpAsync(string ipAddress, string scenario, CancellationToken cancellationToken = default);
}
