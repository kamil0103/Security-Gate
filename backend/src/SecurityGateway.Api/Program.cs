using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecurityGateway.Api.Identity;
using SecurityGateway.Api.Middleware;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Application.Health;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Dashboard;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.Waf;
using SecurityGateway.Infrastructure.Gateway;
using SecurityGateway.Infrastructure.Health;
using SecurityGateway.Infrastructure.Identity;
using StackExchange.Redis;
using SecurityGateway.Infrastructure.IpIntelligence;
using SecurityGateway.Infrastructure.IpIntelligence.Providers;
using SecurityGateway.Infrastructure.IpIntelligence.Repositories;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.AccessControl.Services;
using SecurityGateway.Infrastructure.Applications.Repositories;
using SecurityGateway.Infrastructure.Applications.Services;
using SecurityGateway.Infrastructure.Blocking.Services;
using SecurityGateway.Infrastructure.Dashboard.Services;
using SecurityGateway.Infrastructure.RateLimiting.Repositories;
using SecurityGateway.Infrastructure.RateLimiting.Services;
using SecurityGateway.Infrastructure.RateLimiting.Stores;
using SecurityGateway.Infrastructure.ThreatDetection.Repositories;
using SecurityGateway.Infrastructure.ThreatDetection.Services;
using SecurityGateway.Infrastructure.Waf.Repositories;
using SecurityGateway.Infrastructure.Waf.Services;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is not configured.");

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string is not configured.");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IHealthCheckService>(_ => new HealthCheckService(postgresConnectionString, redisConnectionString));

var gatewayOptions = builder.Configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>()
    ?? new GatewayOptions();

builder.Services.AddSingleton(gatewayOptions);

var automaticBlockingOptions = builder.Configuration.GetSection(AutomaticBlockingOptions.SectionName).Get<AutomaticBlockingOptions>()
    ?? new AutomaticBlockingOptions();

builder.Services.AddSingleton(automaticBlockingOptions);
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

// Application policy services
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IApplicationPolicyRepository, ApplicationPolicyRepository>();
builder.Services.AddScoped<IApplicationPolicyService, ApplicationPolicyService>();

// Rate limiting services
builder.Services.AddSingleton<IRateLimitStore, RedisRateLimitStore>();
builder.Services.AddScoped<IRateLimitRuleRepository, RateLimitRuleRepository>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();

// WAF services
builder.Services.AddSingleton<IAttackClassifier, ModSecurityAttackClassifier>();
builder.Services.AddScoped<IWafEventRepository, WafEventRepository>();
builder.Services.AddScoped<IWafEventService, WafEventService>();

// Threat detection services
builder.Services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
builder.Services.AddScoped<IThreatScoreRuleRepository, ThreatScoreRuleRepository>();
builder.Services.AddScoped<IThreatDetectionService, ThreatDetectionService>();

// Automatic blocking service
builder.Services.AddScoped<IAutomaticBlockingService, AutomaticBlockingService>();

// Dashboard service
builder.Services.AddScoped<IDashboardService, DashboardService>();

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
