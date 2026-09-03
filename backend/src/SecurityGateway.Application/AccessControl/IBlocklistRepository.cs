using SecurityGateway.Domain.AccessControl;

namespace SecurityGateway.Application.AccessControl;

public interface IBlocklistRepository
{
    Task<BlocklistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BlocklistEntry?> GetByTypeAndValueAsync(BlocklistEntryType type, string value, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlocklistEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlocklistEntry>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(BlocklistEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(BlocklistEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(BlocklistEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts <paramref name="entry"/> if no row with the same (<see cref="BlocklistEntry.Type"/>, <see cref="BlocklistEntry.Value"/>)
    /// exists; otherwise atomically updates the existing row's <see cref="BlocklistEntry.IsEnabled"/>,
    /// <see cref="BlocklistEntry.ExpiresAt"/>, and <see cref="BlocklistEntry.Reason"/> to the values from <paramref name="entry"/>.
    /// The operation is concurrency-safe on PostgreSQL.
    /// </summary>
    Task UpsertAsync(BlocklistEntry entry, CancellationToken cancellationToken = default);
}
