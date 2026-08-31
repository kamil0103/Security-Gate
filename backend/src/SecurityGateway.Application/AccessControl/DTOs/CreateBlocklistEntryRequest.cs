using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl.DTOs;

public sealed record CreateBlocklistEntryRequest
{
    public required BlocklistEntryType Type { get; init; }
    public required string Value { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
