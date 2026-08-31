using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SecurityGateway.Application.ThreatIntelligence;

namespace SecurityGateway.Infrastructure.ThreatIntelligence.Providers;

public sealed class AbuseIpDbThreatIntelligenceProvider : IThreatIntelligenceProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AbuseIpDbThreatIntelligenceProvider(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public string Name => "AbuseIPDB";

    public async Task<ThreatIntelligenceResult> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new ThreatIntelligenceResult { Source = Name };
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.abuseipdb.com/api/v2/check?ipAddress={Uri.EscapeDataString(ipAddress)}&maxAgeInDays=90");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return new ThreatIntelligenceResult
            {
                Source = Name,
                RawData = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
            };
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken).ConfigureAwait(false);

        if (!json.TryGetProperty("data", out var data))
        {
            return new ThreatIntelligenceResult { Source = Name };
        }

        var abuseConfidence = data.GetProperty("abuseConfidenceScore").GetInt32();
        var categories = new List<string>();

        if (data.TryGetProperty("usageType", out var usageType))
        {
            categories.Add(usageType.GetString() ?? "Unknown");
        }

        var result = new ThreatIntelligenceResult
        {
            Source = Name,
            IsMalicious = abuseConfidence >= 25,
            ConfidenceScore = abuseConfidence,
            Categories = categories,
            CountryCode = data.TryGetProperty("countryCode", out var countryCode) ? countryCode.GetString() : null,
            Isp = data.TryGetProperty("isp", out var isp) ? isp.GetString() : null,
            RawData = json.ToString()
        };

        return result;
    }
}
