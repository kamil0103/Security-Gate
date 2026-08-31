namespace SecurityGateway.Domain.Applications;

public sealed class Application
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Domain { get; set; }
    public required string UpstreamUrl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ApplicationCloudflareMode CloudflareMode { get; set; } = ApplicationCloudflareMode.Proxied;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public ApplicationPolicy? Policy { get; set; }
}
