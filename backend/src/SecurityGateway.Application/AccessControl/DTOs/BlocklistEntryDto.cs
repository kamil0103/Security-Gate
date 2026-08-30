using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed record BlocklistEntryDto
{
    public required Guid Id { get; init; }
    public required BlocklistEntryType Type { get; init; }
    public required string Value { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsEnabled { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
