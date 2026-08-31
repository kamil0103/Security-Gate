namespace SecurityGateway.Application.Cloudflare;

public interface ICloudflareIpService
{
    bool IsCloudflareIp(string ipAddress);
    Task RefreshRangesAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<string> GetRanges();
}
