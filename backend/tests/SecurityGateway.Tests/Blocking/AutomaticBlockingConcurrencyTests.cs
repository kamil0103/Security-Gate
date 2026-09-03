using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Blocking.DTOs;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.Blocking.Services;
using SecurityGateway.Infrastructure.Identity;
using SecurityGateway.Infrastructure.IpIntelligence.Repositories;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Tests.Helpers;
using Testcontainers.PostgreSql;
using Xunit;

namespace SecurityGateway.Tests.Blocking;

public sealed class AutomaticBlockingConcurrencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    public AutomaticBlockingConcurrencyTests()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("securitygateway_concurrency")
            .WithUsername("sg_test")
            .WithPassword("sg_test_pwd")
            .Build();
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task BlockAsync_ConcurrentDuplicateIpRequests_CreateExactlyOneActiveBlock()
    {
        // Arrange
        const string ipAddress = "203.0.113.77";
        const int concurrency = 50;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), npgsql =>
                npgsql.MigrationsAssembly("SecurityGateway.Infrastructure"))
            .Options;

        await using (var setupContext = new ApplicationDbContext(options))
        {
            await setupContext.Database.MigrateAsync();
        }

        var blockingOptions = new AutomaticBlockingOptions
        {
            Enabled = true,
            MediumThreshold = 40,
            HighThreshold = 60,
            CriticalThreshold = 80,
            MediumBlockDurationMinutes = 30,
            HighBlockDurationMinutes = 240,
            CriticalBlockDurationMinutes = 1440
        };

        var tasks = new List<Task<BlockResultDto>>();
        var exceptions = new List<Exception>();

        // Act: fire many concurrent blocking requests for the same IP.
        // Each request gets its own DbContext to simulate real per-request scoping.
        for (var i = 0; i < concurrency; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await using var context = new ApplicationDbContext(options);
                    var repository = new BlocklistRepository(context);
                    var ipAddressRepository = new IpAddressRepository(context);
                    var trustedNetworkRepository = new TrustedNetworkRepository(context);
                    var service = new AutomaticBlockingService(
                        repository,
                        ipAddressRepository,
                        trustedNetworkRepository,
                        context,
                        blockingOptions,
                        new FakeAuditService());

                    return await service.BlockAsync(ipAddress, 60, "Concurrency test block");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    throw;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        await using var assertContext = new ApplicationDbContext(options);
        var entries = assertContext.BlocklistEntries
            .Where(e => e.Type == Domain.AccessControl.BlocklistEntryType.Ip && e.Value == ipAddress)
            .ToList();

        Assert.Empty(exceptions);
        Assert.Single(entries);
        Assert.True(entries[0].IsEnabled);
        Assert.Equal("Concurrency test block", entries[0].Reason);
        Assert.NotNull(entries[0].ExpiresAt);
        Assert.True(entries[0].ExpiresAt > DateTimeOffset.UtcNow);
        Assert.All(results, r => Assert.True(r.Blocked));
        Assert.All(results, r => Assert.Equal(ipAddress, r.IpAddress));
    }
}
