using SecurityGateway.Application.ThreatIntelligence;
using SecurityGateway.Infrastructure.ThreatIntelligence.Services;
using Xunit;

namespace SecurityGateway.Tests.AdvancedSecurity;

public class ThreatIntelligenceServiceTests
{
    [Fact]
    public async Task LookupAsync_AggregatesProviderResults()
    {
        var providers = new List<IThreatIntelligenceProvider>
        {
            new FakeProvider("A", true, 80),
            new FakeProvider("B", false, 0)
        };

        var service = new ThreatIntelligenceService(providers);
        var results = await service.LookupAsync("1.1.1.1");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Source == "A" && r.IsMalicious);
    }

    [Fact]
    public async Task LookupAsync_FailingProvider_IsIgnored()
    {
        var providers = new List<IThreatIntelligenceProvider>
        {
            new FailingProvider(),
            new FakeProvider("A", true, 80)
        };

        var service = new ThreatIntelligenceService(providers);
        var results = await service.LookupAsync("1.1.1.1");

        Assert.Single(results);
    }

    private sealed class FakeProvider : IThreatIntelligenceProvider
    {
        public FakeProvider(string name, bool malicious, int score)
        {
            Name = name;
            _malicious = malicious;
            _score = score;
        }

        public string Name { get; }
        private readonly bool _malicious;
        private readonly int _score;

        public Task<ThreatIntelligenceResult> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
            => Task.FromResult(new ThreatIntelligenceResult
            {
                Source = Name,
                IsMalicious = _malicious,
                ConfidenceScore = _score
            });
    }

    private sealed class FailingProvider : IThreatIntelligenceProvider
    {
        public string Name => "Failing";

        public Task<ThreatIntelligenceResult> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Provider failure");
    }
}
