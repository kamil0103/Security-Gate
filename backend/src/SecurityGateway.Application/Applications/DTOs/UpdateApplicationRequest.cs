namespace SecurityGateway.Application.Applications.DTOs;

public sealed record UpdateApplicationRequest
{
    public required string Name { get; init; }
    public required string Domain { get; init; }
    public required string UpstreamUrl { get; init; }
    public required bool IsEnabled { get; init; }
}
