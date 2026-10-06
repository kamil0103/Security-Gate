using Microsoft.EntityFrameworkCore;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Infrastructure.Persistence;

namespace SecurityGateway.Api.AccessControl;

/// <summary>
/// Marks expired pending challenges in small batches without loading entities or
/// racing against normal request processing through tracked EF entities.
/// </summary>
public sealed class ExpiredAccessRequestCleanupService : BackgroundService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredAccessRequestCleanupService> _logger;

    public ExpiredAccessRequestCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredAccessRequestCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Do not run the cleanup loop against the InMemory test provider.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire pending access requests; retrying later");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task<int> ExpireBatchAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;

        // Each iteration is a single conditional SQL UPDATE; concurrent approvals
        // that have already changed status will not be overwritten.
        var ids = db.AccessRequests
            .Where(r => r.Status == AccessRequestStatus.Pending && r.ExpiresAt <= now)
            .OrderBy(r => r.ExpiresAt)
            .Select(r => r.Id)
            .Take(BatchSize);

        var expired = await db.AccessRequests
            .Where(r => ids.Contains(r.Id) && r.Status == AccessRequestStatus.Pending && r.ExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, AccessRequestStatus.Expired)
                .SetProperty(r => r.UpdatedAt, now), cancellationToken);

        if (expired > 0)
        {
            _logger.LogInformation("Expired {Count} stale access requests", expired);
        }

        return expired;
    }
}
