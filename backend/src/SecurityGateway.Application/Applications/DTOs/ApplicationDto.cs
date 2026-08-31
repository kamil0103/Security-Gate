namespace SecurityGateway.Application.Applications.DTOs;

public sealed record ApplicationDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Domain { get; init; }
    public required string UpstreamUrl { get; init; }
    public bool IsEnabled { get; init; }
    public string CloudflareMode { get; init; } = "Proxied";
    public DateTimeOffset CreatedAt { get; init; }
    public ApplicationPolicyDto? Policy { get; init; }
}
