namespace SecurityGateway.Application.Applications.DTOs;

public sealed record ApplicationPolicyDto
{
    public required Guid Id { get; init; }
    public required Guid ApplicationId { get; init; }
    public bool RequireAuthentication { get; init; }
    public bool AllowAnonymousFromTrustedNetworks { get; init; }
    public string AllowedCountries { get; init; } = string.Empty;
    public string BlockedCountries { get; init; } = string.Empty;
    public string AllowedIpAddresses { get; init; } = string.Empty;
    public string BlockedIpAddresses { get; init; } = string.Empty;
}
