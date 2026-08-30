using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecurityGateway.Api.Identity;
using SecurityGateway.Api.Middleware;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Application.Health;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Infrastructure.Gateway;
using SecurityGateway.Infrastructure.Health;
using SecurityGateway.Infrastructure.Identity;
using SecurityGateway.Infrastructure.IpIntelligence;
using SecurityGateway.Infrastructure.IpIntelligence.Providers;
using SecurityGateway.Infrastructure.IpIntelligence.Repositories;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.AccessControl.Services;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is not configured.");

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string is not configured.");

builder.Services.AddSingleton<IHealthCheckService>(_ => new HealthCheckService(postgresConnectionString, redisConnectionString));

var gatewayOptions = builder.Configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>()
    ?? new GatewayOptions();

builder.Services.AddSingleton(gatewayOptions);
builder.Services.AddSingleton<IClientIpResolver>(_ => new ForwardedHeadersClientIpResolver(gatewayOptions.TrustedProxies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));

builder.Services.AddHttpClient<IProxyService, HttpClientProxyService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<GatewayOptions>();
    client.BaseAddress = new Uri(options.UpstreamNpmUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    UseProxy = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
});

// Database
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(postgresConnectionString, npgsql =>
            npgsql.MigrationsAssembly("SecurityGateway.Infrastructure")));
}

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Identity repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IIpAddressRepository, IpAddressRepository>();
builder.Services.AddScoped<ITrustedNetworkRepository, TrustedNetworkRepository>();
builder.Services.AddScoped<IBlocklistRepository, BlocklistRepository>();
builder.Services.AddScoped<IAccessDecisionRepository, AccessDecisionRepository>();

// Identity services
builder.Services.AddScoped<IDeviceIdentityService, DeviceIdentityService>();
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

// Access control services
builder.Services.AddScoped<IAccessControlService, AccessControlService>();

// IP intelligence providers (replace with real providers in production)
builder.Services.AddSingleton<IGeoIpProvider, NullGeoIpProvider>();
builder.Services.AddSingleton<IReputationProvider, NullReputationProvider>();
builder.Services.AddSingleton<IVpnProxyDetector, NullVpnProxyDetector>();

builder.Services.AddScoped<IIpIntelligenceService, IpIntelligenceService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT options are not configured.");

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

var smtpOptions = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>()
    ?? new SmtpOptions();

builder.Services.AddSingleton(smtpOptions);
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

var defaultAdminOptions = builder.Configuration.GetSection(DefaultAdminOptions.SectionName).Get<DefaultAdminOptions>()
    ?? new DefaultAdminOptions();

builder.Services.AddSingleton(defaultAdminOptions);

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3100",
                "http://127.0.0.1:3100")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevelopmentCors");

app.UseMiddleware<GatewayMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed default admin user
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var adminOptions = scope.ServiceProvider.GetRequiredService<DefaultAdminOptions>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var seeder = new DataSeeder(context, passwordHasher, adminOptions);

        try
        {
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed the database. The application will continue starting.");
        }
}

app.Run();

public partial class Program
{
}
