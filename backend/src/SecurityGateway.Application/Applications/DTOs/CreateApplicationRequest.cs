namespace SecurityGateway.Application.Applications.DTOs;

public sealed record CreateApplicationRequest
{
    public required string Name { get; init; }
    public required string Domain { get; init; }
    public required string UpstreamUrl { get; init; }
}
