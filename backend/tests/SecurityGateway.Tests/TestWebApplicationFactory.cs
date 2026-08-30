using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Tests.TestHelpers;
using StackExchange.Redis;

namespace SecurityGateway.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing ApplicationDbContext registration.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("SecurityGatewayTestDb"));

            // Ensure a strong JWT secret is available for tests.
            var jwtDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(JwtOptions));
            if (jwtDescriptor is not null)
            {
                services.Remove(jwtDescriptor);
            }

            services.AddSingleton(new JwtOptions
            {
                Secret = "ThisIsATestSecretKeyThatIsAtLeast32CharsLong!",
                Issuer = "SecurityGateway",
                Audience = "SecurityGateway",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            });

            // Replace Redis-backed rate limiting with an in-memory store for tests.
            var connectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (connectionDescriptor is not null)
            {
                services.Remove(connectionDescriptor);
            }

            var rateLimitStoreDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRateLimitStore));
            if (rateLimitStoreDescriptor is not null)
            {
                services.Remove(rateLimitStoreDescriptor);
            }

            services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
        });
    }
}
