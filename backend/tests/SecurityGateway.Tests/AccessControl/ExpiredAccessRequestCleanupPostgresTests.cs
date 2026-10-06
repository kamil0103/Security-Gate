using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SecurityGateway.Api.AccessControl;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;
using AppEntity = SecurityGateway.Domain.Applications.Application;

namespace SecurityGateway.Tests.AccessControl;

public sealed class ExpiredAccessRequestCleanupPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("securitygateway_expiry")
        .WithUsername("sg_test")
        .WithPassword("sg_test_pwd")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Database_RejectsSecondTrustRecordForSameAccessRequest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), n =>
                n.MigrationsAssembly("SecurityGateway.Infrastructure"))
            .Options;
        Guid appId, requestId;
        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var app = new AppEntity
            {
                Name = "Unique trust test",
                Domain = "unique-trust.example.test",
                UpstreamUrl = "http://upstream",
                IsEnabled = true
            };
            setup.Applications.Add(app);
            await setup.SaveChangesAsync();
            var request = new AccessRequest
            {
                ApplicationId = app.Id,
                ClientIp = "198.51.100.10",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
            };
            setup.AccessRequests.Add(request);
            await setup.SaveChangesAsync();
            appId = app.Id;
            requestId = request.Id;
        }

        await using (var first = new ApplicationDbContext(options))
        {
            first.TrustRecords.Add(new TrustRecord
            {
                ApplicationId = appId,
                AccessRequestId = requestId,
                Scope = TrustScope.Session
            });
            await first.SaveChangesAsync();
        }

        await using (var second = new ApplicationDbContext(options))
        {
            second.TrustRecords.Add(new TrustRecord
            {
                ApplicationId = appId,
                AccessRequestId = requestId,
                Scope = TrustScope.Session
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        }

        await using var verify = new ApplicationDbContext(options);
        Assert.Equal(1, await verify.TrustRecords.CountAsync(r => r.AccessRequestId == requestId));
    }

    [Fact]
    public async Task ExpireBatchAsync_OnlyExpiresStalePendingRequests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), n =>
                n.MigrationsAssembly("SecurityGateway.Infrastructure"))
            .Options;

        var now = DateTimeOffset.UtcNow;
        Guid expiredId, approvedId, liveId;
        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var app = new AppEntity
            {
                Name = "Expiry test",
                Domain = "expiry.example.test",
                UpstreamUrl = "http://upstream",
                IsEnabled = true
            };
            setup.Applications.Add(app);
            await setup.SaveChangesAsync();

            var expired = new AccessRequest
            {
                ApplicationId = app.Id,
                ClientIp = "198.51.100.10",
                ExpiresAt = now.AddMinutes(-5),
                Status = AccessRequestStatus.Pending
            };
            var approved = new AccessRequest
            {
                ApplicationId = app.Id,
                ClientIp = "198.51.100.11",
                ExpiresAt = now.AddMinutes(-5),
                Status = AccessRequestStatus.Approved
            };
            var live = new AccessRequest
            {
                ApplicationId = app.Id,
                ClientIp = "198.51.100.12",
                ExpiresAt = now.AddMinutes(5),
                Status = AccessRequestStatus.Pending
            };
            setup.AccessRequests.AddRange(expired, approved, live);
            await setup.SaveChangesAsync();
            expiredId = expired.Id;
            approvedId = approved.Id;
            liveId = live.Id;
        }

        using var provider = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()))
            .BuildServiceProvider();
        var worker = new ExpiredAccessRequestCleanupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ExpiredAccessRequestCleanupService>.Instance);

        Assert.Equal(1, await worker.ExpireBatchAsync());
        Assert.Equal(0, await worker.ExpireBatchAsync());

        await using var verify = new ApplicationDbContext(options);
        Assert.Equal(AccessRequestStatus.Expired,
            (await verify.AccessRequests.SingleAsync(r => r.Id == expiredId)).Status);
        Assert.Equal(AccessRequestStatus.Approved,
            (await verify.AccessRequests.SingleAsync(r => r.Id == approvedId)).Status);
        Assert.Equal(AccessRequestStatus.Pending,
            (await verify.AccessRequests.SingleAsync(r => r.Id == liveId)).Status);
    }
}
