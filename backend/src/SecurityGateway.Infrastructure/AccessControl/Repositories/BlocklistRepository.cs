using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Infrastructure.AccessControl.Repositories;

public sealed class BlocklistRepository : IBlocklistRepository
{
    private readonly ApplicationDbContext _context;

    private const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public BlocklistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<BlocklistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.BlocklistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<BlocklistEntry?> GetByTypeAndValueAsync(BlocklistEntryType type, string value, CancellationToken cancellationToken = default)
    {
        return _context.BlocklistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Type == type && e.Value == value, cancellationToken);
    }

    public async Task<IReadOnlyList<BlocklistEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _context.BlocklistEntries
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries.AsReadOnly();
    }

    public async Task<IReadOnlyList<BlocklistEntry>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entries = await _context.BlocklistEntries
            .AsNoTracking()
            .Where(e => e.IsEnabled && (e.ExpiresAt == null || e.ExpiresAt > now))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries.AsReadOnly();
    }

    public async Task AddAsync(BlocklistEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.BlocklistEntries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(BlocklistEntry entry, CancellationToken cancellationToken = default)
    {
        _context.BlocklistEntries.Update(entry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BlocklistEntry entry, CancellationToken cancellationToken = default)
    {
        var tracked = _context.BlocklistEntries.Local.FirstOrDefault(e => e.Id == entry.Id);

        if (tracked is not null)
        {
            _context.BlocklistEntries.Remove(tracked);
        }
        else
        {
            _context.BlocklistEntries.Remove(entry);
        }

        return Task.CompletedTask;
    }

    public async Task UpsertAsync(BlocklistEntry entry, CancellationToken cancellationToken = default)
    {
        if (_context.Database.ProviderName == PostgresProviderName)
        {
            // PostgreSQL-native atomic upsert. The unique index on (Type, Value) is the conflict target.
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""BlocklistEntries"" (""Id"", ""Type"", ""Value"", ""Reason"", ""ExpiresAt"", ""IsEnabled"", ""CreatedByUserId"", ""CreatedAt"")
                VALUES ({entry.Id}, {(int)entry.Type}, {entry.Value}, {entry.Reason}, {entry.ExpiresAt}, {entry.IsEnabled}, {entry.CreatedByUserId}, {entry.CreatedAt})
                ON CONFLICT (""Type"", ""Value"")
                DO UPDATE SET
                    ""IsEnabled"" = EXCLUDED.""IsEnabled"",
                    ""ExpiresAt"" = EXCLUDED.""ExpiresAt"",
                    ""Reason"" = EXCLUDED.""Reason"";",
                cancellationToken).ConfigureAwait(false);

            return;
        }

        // Non-PostgreSQL fallback (e.g., in-memory EF tests) using the original check-then-update pattern.
        var existing = await GetByTypeAndValueAsync(entry.Type, entry.Value, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.IsEnabled = entry.IsEnabled;
            existing.ExpiresAt = entry.ExpiresAt;
            existing.Reason = entry.Reason;

            await UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await AddAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }
}
