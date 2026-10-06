using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;
using Xunit;

namespace SecurityGateway.Tests.Identity;

public sealed class DeviceRecognitionPostgresConcurrencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("securitygateway_devices")
        .WithUsername("sg_test")
        .WithPassword("sg_test_pwd")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task ConcurrentRecognition_ExistingBlockedDevice_DoesNotWriteOrChangeTrust()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), n =>
                n.MigrationsAssembly("SecurityGateway.Infrastructure"))
            .Options;
        var user = new User { Username = "device-test", Email = "device-test@example.test", PasswordHash = "hash" };
        var device = new Device
        {
            UserId = user.Id,
            Name = "Test device",
            Fingerprint = "fingerprint-123",
            CredentialId = "client-device-123",
            TrustStatus = DeviceTrustStatus.Blocked
        };
        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Users.Add(user);
            setup.Devices.Add(device);
            await setup.SaveChangesAsync();
        }

        DateTimeOffset persistedLastSeen;
        await using (var baseline = new ApplicationDbContext(options))
        {
            persistedLastSeen = (await baseline.Devices.SingleAsync(d => d.Id == device.Id)).LastSeenAt;
        }

        var request = new DeviceEnrollmentRequest
        {
            DeviceId = "client-device-123",
            Name = "Test device",
            Fingerprint = "fingerprint-123"
        };
        var results = await Task.WhenAll(Enumerable.Range(0, 30).Select(async _ =>
        {
            await using var db = new ApplicationDbContext(options);
            var service = new DeviceIdentityService(new DeviceRepository(db), db);
            return await service.RecognizeOrEnrollAsync(user.Id, request, "198.51.100.10");
        }));

        Assert.All(results, r =>
        {
            Assert.Equal(DeviceTrustStatus.Blocked, r.TrustStatus);
            Assert.False(r.IsTrusted);
        });
        await using var verify = new ApplicationDbContext(options);
        var persisted = await verify.Devices.SingleAsync(d => d.Id == device.Id);
        Assert.Equal(DeviceTrustStatus.Blocked, persisted.TrustStatus);
        Assert.Equal(persistedLastSeen, persisted.LastSeenAt);
    }
}
