using SecurityGateway.Application.CrowdSec;

namespace SecurityGateway.Infrastructure.CrowdSec;

public sealed class CrowdSecClient : ICrowdSecClient
{
    private readonly CrowdSecOptions _options;

    public CrowdSecClient(CrowdSecOptions options)
    {
        _options = options;
    }

    public Task<bool> IsIpMaliciousAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(false);
        }

        // Real integration requires the CrowdSec local API at /v1/decisions.
        // This stub returns false and can be replaced with an HTTP client call.
        return Task.FromResult(false);
    }

    public Task ReportIpAsync(string ipAddress, string scenario, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        // Real integration requires posting alerts to the CrowdSec local API.
        return Task.CompletedTask;
    }
}
