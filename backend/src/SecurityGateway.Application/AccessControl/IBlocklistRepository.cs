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
}
