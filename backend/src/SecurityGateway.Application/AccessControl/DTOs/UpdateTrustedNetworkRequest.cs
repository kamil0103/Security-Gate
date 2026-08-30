namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed record UpdateTrustedNetworkRequest
{
    public required string Name { get; init; }
    public required string Cidr { get; init; }
    public string? Description { get; init; }
    public required bool IsEnabled { get; init; }
}
