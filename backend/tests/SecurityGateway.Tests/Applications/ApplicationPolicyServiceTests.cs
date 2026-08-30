using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Applications.DTOs;
using SecurityGateway.Infrastructure.Applications.Repositories;
using SecurityGateway.Infrastructure.Applications.Services;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.Applications;

public class ApplicationPolicyServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ApplicationPolicyService _service;

    public ApplicationPolicyServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var appRepository = new ApplicationRepository(_context);
        var policyRepository = new ApplicationPolicyRepository(_context);

        _service = new ApplicationPolicyService(appRepository, policyRepository, _context);
    }

    [Fact]
    public async Task CreateApplicationAsync_ValidRequest_CreatesApplicationAndDefaultPolicy()
    {
        var result = await _service.CreateApplicationAsync(new CreateApplicationRequest
        {
            Name = "Immich",
            Domain = "photos.example.com",
            UpstreamUrl = "http://localhost:3001"
        });

        Assert.NotNull(result);
        Assert.Equal("Immich", result.Name);
        Assert.Equal("photos.example.com", result.Domain);
        Assert.NotNull(result.Policy);
        Assert.True(result.Policy.RequireAuthentication);
    }

    [Fact]
    public async Task CreateApplicationAsync_DuplicateDomain_ThrowsException()
    {
        var request = new CreateApplicationRequest
        {
            Name = "Immich",
            Domain = "photos.example.com",
            UpstreamUrl = "http://localhost:3001"
        };

        await _service.CreateApplicationAsync(request);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateApplicationAsync(request));
    }

    [Fact]
    public async Task EvaluatePolicyAsync_DisabledApplication_Denies()
    {
        var app = await _service.CreateApplicationAsync(new CreateApplicationRequest
        {
            Name = "Test",
            Domain = "test.example.com",
            UpstreamUrl = "http://localhost:3001"
        });

        await _service.UpdateApplicationAsync(app.Id, new UpdateApplicationRequest
        {
            Name = app.Name,
            Domain = app.Domain,
            UpstreamUrl = app.UpstreamUrl,
            IsEnabled = false
        });

        var evaluation = await _service.EvaluatePolicyAsync(app.Id, "10.0.0.1", true, false);

        Assert.False(evaluation.Allowed);
        Assert.Contains("disabled", evaluation.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_RequireAuthentication_Unauthenticated_Denies()
    {
        var app = await _service.CreateApplicationAsync(new CreateApplicationRequest
        {
            Name = "Test",
            Domain = "test.example.com",
            UpstreamUrl = "http://localhost:3001"
        });

        var evaluation = await _service.EvaluatePolicyAsync(app.Id, "10.0.0.1", false, false);

        Assert.False(evaluation.Allowed);
        Assert.True(evaluation.RequiresAuthentication);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_TrustedNetworkAnonymous_Allows()
    {
        var app = await _service.CreateApplicationAsync(new CreateApplicationRequest
        {
            Name = "Test",
            Domain = "test.example.com",
            UpstreamUrl = "http://localhost:3001"
        });

        await _service.UpdatePolicyAsync(app.Id, new UpdateApplicationPolicyRequest
        {
            RequireAuthentication = true,
            AllowAnonymousFromTrustedNetworks = true
        });

        var evaluation = await _service.EvaluatePolicyAsync(app.Id, "10.0.0.1", false, true);

        Assert.True(evaluation.Allowed);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_BlockedIp_Denies()
    {
        var app = await _service.CreateApplicationAsync(new CreateApplicationRequest
        {
            Name = "Test",
            Domain = "test.example.com",
            UpstreamUrl = "http://localhost:3001"
        });

        await _service.UpdatePolicyAsync(app.Id, new UpdateApplicationPolicyRequest
        {
            RequireAuthentication = true,
            AllowAnonymousFromTrustedNetworks = false,
            BlockedIpAddresses = "198.51.100.10"
        });

        var evaluation = await _service.EvaluatePolicyAsync(app.Id, "198.51.100.10", true, false);

        Assert.False(evaluation.Allowed);
        Assert.Contains("blocked", evaluation.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_AllowedIp_NotInList_Denies()
    {
        var app = await _service.CreateApplicationAsync(new CreateApplicationRequest
        {
            Name = "Test",
            Domain = "test.example.com",
            UpstreamUrl = "http://localhost:3001"
        });

        await _service.UpdatePolicyAsync(app.Id, new UpdateApplicationPolicyRequest
        {
            RequireAuthentication = true,
            AllowAnonymousFromTrustedNetworks = false,
            AllowedIpAddresses = "10.0.0.1"
        });

        var evaluation = await _service.EvaluatePolicyAsync(app.Id, "10.0.0.2", true, false);

        Assert.False(evaluation.Allowed);
    }

    [Fact]
    public async Task UpdatePolicyAsync_CreatesPolicyWhenMissing()
    {
        var app = await _service.CreateApplicationAsync(new CreateApplicationRequest
        {
            Name = "Test",
            Domain = "test.example.com",
            UpstreamUrl = "http://localhost:3001"
        });

        var policy = await _service.UpdatePolicyAsync(app.Id, new UpdateApplicationPolicyRequest
        {
            RequireAuthentication = false,
            AllowAnonymousFromTrustedNetworks = false
        });

        Assert.False(policy.RequireAuthentication);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
