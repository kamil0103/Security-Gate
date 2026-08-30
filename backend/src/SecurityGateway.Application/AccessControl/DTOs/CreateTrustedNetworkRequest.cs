namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed record CreateTrustedNetworkRequest
{
    public required string Name { get; init; }
    public required string Cidr { get; init; }
    public string? Description { get; init; }
}
