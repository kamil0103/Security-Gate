namespace SecurityGateway.Domain.AccessControl;

public sealed class BlocklistEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required BlocklistEntryType Type { get; init; }
    public required string Value { get; init; }
    public string? Reason { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
