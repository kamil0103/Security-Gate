namespace SecurityGateway.Domain.Applications;

public sealed class ApplicationPolicy
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ApplicationId { get; init; }
    public Application Application { get; init; } = null!;

    public bool RequireAuthentication { get; set; } = true;
    public bool AllowAnonymousFromTrustedNetworks { get; set; } = false;

    public string AllowedCountries { get; set; } = string.Empty;
    public string BlockedCountries { get; set; } = string.Empty;
    public string AllowedIpAddresses { get; set; } = string.Empty;
    public string BlockedIpAddresses { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
