namespace SecurityGateway.Application.Applications.DTOs;

public sealed record UpdateApplicationPolicyRequest
{
    public required bool RequireAuthentication { get; init; }
    public required bool AllowAnonymousFromTrustedNetworks { get; init; }
    public string AllowedCountries { get; init; } = string.Empty;
    public string BlockedCountries { get; init; } = string.Empty;
    public string AllowedIpAddresses { get; init; } = string.Empty;
    public string BlockedIpAddresses { get; init; } = string.Empty;
}
