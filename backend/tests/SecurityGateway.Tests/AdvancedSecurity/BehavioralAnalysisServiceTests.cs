using SecurityGateway.Application.BehavioralAnalysis;
using SecurityGateway.Infrastructure.BehavioralAnalysis.Services;
using Xunit;

namespace SecurityGateway.Tests.AdvancedSecurity;

public class BehavioralAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_Disabled_ReturnsNoRisk()
    {
        var service = new BehavioralAnalysisService(new BehavioralAnalysisOptions { Enabled = false });

        var result = await service.AnalyzeAsync(new BehavioralRequest
        {
            IpAddress = "1.1.1.1",
            Path = "/",
            Method = "GET"
        });

        Assert.False(result.IsAnomalous);
        Assert.Equal(0, result.RiskScore);
    }

    [Fact]
    public async Task AnalyzeAsync_BurstDetected_ReturnsRisk()
    {
        var service = new BehavioralAnalysisService(new BehavioralAnalysisOptions
        {
            Enabled = true,
            WindowSeconds = 60,
            BurstThreshold = 3
        });

        for (var i = 0; i < 5; i++)
        {
            await service.AnalyzeAsync(new BehavioralRequest
            {
                IpAddress = "1.1.1.1",
                Path = "/",
                Method = "GET"
            });
        }

        var result = await service.AnalyzeAsync(new BehavioralRequest
        {
            IpAddress = "1.1.1.1",
            Path = "/",
            Method = "GET"
        });

        Assert.True(result.IsAnomalous);
        Assert.True(result.RiskScore > 0);
        Assert.Contains("Request burst detected", result.Reasons[0]);
    }
}
