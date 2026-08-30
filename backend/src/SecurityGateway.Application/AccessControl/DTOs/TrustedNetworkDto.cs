namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed record TrustedNetworkDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Cidr { get; init; }
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
