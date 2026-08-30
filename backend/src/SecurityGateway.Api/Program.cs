using SecurityGateway.Api.Middleware;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Application.Health;
using SecurityGateway.Infrastructure.Gateway;
using SecurityGateway.Infrastructure.Health;

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
    // Allow proxying of streaming responses without buffering.
    UseProxy = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
});

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

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
