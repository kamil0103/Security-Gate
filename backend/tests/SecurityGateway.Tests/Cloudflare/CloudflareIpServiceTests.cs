using Microsoft.Extensions.Logging.Abstractions;
using SecurityGateway.Application.Cloudflare;
using SecurityGateway.Infrastructure.Cloudflare;
using Xunit;

namespace SecurityGateway.Tests.Cloudflare;

public class CloudflareIpServiceTests
{
    private static CloudflareIpService CreateService(bool enabled = true, params string[] ranges)
    {
        var options = new CloudflareOptions { Enabled = enabled };

        foreach (var range in ranges)
        {
            options.IpRanges.Add(range);
        }

        return new CloudflareIpService(options, NullLogger<CloudflareIpService>.Instance);
    }

    [Fact]
    public void IsCloudflareIp_WhenDisabled_ReturnsFalse()
    {
        var service = CreateService(enabled: false);

        Assert.False(service.IsCloudflareIp("104.16.0.1"));
    }

    [Fact]
    public void IsCloudflareIp_KnownDefaultRange_ReturnsTrue()
    {
        var service = CreateService();

        Assert.True(service.IsCloudflareIp("104.16.0.1"));
    }

    [Fact]
    public void IsCloudflareIp_OutsideDefaultRange_ReturnsFalse()
    {
        var service = CreateService();

        Assert.False(service.IsCloudflareIp("203.0.113.1"));
    }

    [Fact]
    public void IsCloudflareIp_CustomRange_Matches()
    {
        var service = CreateService(ranges: "192.168.5.0/24");

        Assert.True(service.IsCloudflareIp("192.168.5.1"));
        Assert.False(service.IsCloudflareIp("192.168.6.1"));
    }

    [Fact]
    public void IsCloudflareIp_InvalidIp_ReturnsFalse()
    {
        var service = CreateService();

        Assert.False(service.IsCloudflareIp("not-an-ip"));
    }

    [Fact]
    public void GetRanges_DefaultRanges_ReturnsKnownRanges()
    {
        var service = CreateService();

        var ranges = service.GetRanges();

        Assert.NotEmpty(ranges);
        Assert.Contains("104.16.0.0/13", ranges);
    }

    [Fact]
    public async Task RefreshRangesAsync_ReturnsCompletedTask()
    {
        var service = CreateService();

        await service.RefreshRangesAsync();

        Assert.True(service.IsCloudflareIp("104.16.0.1"));
    }
}
