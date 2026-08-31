using System.Collections.Concurrent;
using SecurityGateway.Application.BehavioralAnalysis;

namespace SecurityGateway.Infrastructure.BehavioralAnalysis.Services;

public class BehavioralAnalysisService : IBehavioralAnalysisService
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _requests = new();
    private readonly BehavioralAnalysisOptions _options;

    public BehavioralAnalysisService(BehavioralAnalysisOptions options)
    {
        _options = options;
    }

    public Task<BehavioralAnalysisResult> AnalyzeAsync(BehavioralRequest request, CancellationToken cancellationToken = default)
    {
        var result = new BehavioralAnalysisResult();

        if (!_options.Enabled)
        {
            return Task.FromResult(result);
        }

        var now = request.Timestamp;
        var windowStart = now.AddSeconds(-_options.WindowSeconds);

        var timestamps = _requests.AddOrUpdate(
            request.IpAddress,
            _ => new List<DateTimeOffset> { now },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.RemoveAll(t => t < windowStart);
                    existing.Add(now);
                    return existing;
                }
            });

        int count;
        lock (timestamps)
        {
            count = timestamps.Count;
        }

        if (count > _options.BurstThreshold)
        {
            result.IsAnomalous = true;
            result.RiskScore += Math.Min(100, (count - _options.BurstThreshold) * 5);
            result.Reasons.Add($"Request burst detected: {count} requests in {_options.WindowSeconds}s");
        }

        return Task.FromResult(result);
    }
}
