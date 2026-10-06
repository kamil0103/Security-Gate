using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;
using AppEntity = SecurityGateway.Domain.Applications.Application;

namespace SecurityGateway.Tests.AccessControl;

public sealed class ConcurrentApprovalHttpPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("securitygateway_http_approval")
        .WithUsername("sg_test")
        .WithPassword("sg_test_pwd")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task TwoHttpApprovalRequests_OneSucceeds_OneConflicts_OneTrustRecord()
    {
        await using var factory = new PostgreSqlWebFactory(_postgres.GetConnectionString());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        Guid requestId;
        string token;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var admin = new User
            {
                Username = "approvalraceadmin",
                Email = "approvalraceadmin@example.test",
                PasswordHash = hasher.HashPassword("StrongPassword123!"),
                Role = UserRole.Administrator,
                Status = UserStatus.Active,
                EmailVerified = true
            };
            var app = new AppEntity
            {
                Name = "HTTP approval race",
                Domain = "approval-race.example.test",
                UpstreamUrl = "http://upstream",
                IsEnabled = true
            };
            db.Users.Add(admin);
            db.Applications.Add(app);
            await db.SaveChangesAsync();
            var request = new AccessRequest
            {
                ApplicationId = app.Id,
                UserId = admin.Id,
                ClientIp = "198.51.100.10",
                DeviceFingerprint = "fp:http-race",
                SessionId = "http-race-session",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();
            requestId = request.Id;
        }

        using var loginClient = factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/auth/login",
            new LoginWithDeviceRequest
            {
                User = new LoginRequest
                {
                    UsernameOrEmail = "approvalraceadmin",
                    Password = "StrongPassword123!"
                }
            });
        login.EnsureSuccessStatusCode();
        token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Tokens.AccessToken;

        using var clientA = factory.CreateClient();
        using var clientB = factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var decision = new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Device
        };

        using var gate = new ManualResetEventSlim(false);
        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            await Task.Run(() => gate.Wait());
            return await client.PostAsJsonAsync($"/api/access-requests/{requestId}/resolve", decision);
        }

        var a = SendAsync(clientA);
        var b = SendAsync(clientB);
        gate.Set();
        using var responseA = await a;
        using var responseB = await b;
        var codes = new[] { responseA.StatusCode, responseB.StatusCode };
        Assert.Contains(HttpStatusCode.OK, codes);
        Assert.Contains(HttpStatusCode.Conflict, codes);

        using var scope2 = factory.Services.CreateScope();
        var verify = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await verify.TrustRecords.CountAsync(t => t.AccessRequestId == requestId));
        Assert.Equal(AccessRequestStatus.Approved,
            (await verify.AccessRequests.SingleAsync(r => r.Id == requestId)).Status);
    }

    private sealed class PostgreSqlWebFactory(string connectionString) : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o =>
                    o.UseNpgsql(connectionString, n => n.MigrationsAssembly("SecurityGateway.Infrastructure")));
            });
        }
    }
}
