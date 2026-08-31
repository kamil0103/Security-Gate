using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecurityGateway.Api.Middleware;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Blocking.DTOs;
using SecurityGateway.Application.Gateway;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Application.RateLimiting.Models;
using Xunit;

namespace SecurityGateway.Tests.Gateway;

public class GatewayMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AdminPath_CallsNextAndDoesNotProxy()
    {
        var proxyService = new FakeProxyService();
        var resolver = new FakeClientIpResolver();
        var options = new GatewayOptions { AdminPathPrefixes = ["/api"] };
        var nextInvoked = false;

        var middleware = new GatewayMiddleware(
            _ => { nextInvoked = true; return Task.CompletedTask; },
            proxyService,
            resolver,
            null,
            CreateApplicationPolicyService(),
            CreateAccessControlService(),
            CreateRateLimitService(),
            CreateAutomaticBlockingService(),
            options,
            NullLogger<GatewayMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/health";

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
        Assert.False(proxyService.WasCalled);
    }

    [Fact]
    public async Task InvokeAsync_ProxiedPath_ForwardsRequest()
    {
        var proxyService = new FakeProxyService();
        var resolver = new FakeClientIpResolver { Result = new ClientIpResolutionResult { ClientIp = "198.51.100.1", ProxyChain = [], IsTrusted = true } };
        var options = new GatewayOptions { AdminPathPrefixes = ["/api"] };

        var middleware = new GatewayMiddleware(
            _ => Task.CompletedTask,
            proxyService,
            resolver,
            null,
            CreateApplicationPolicyService(),
            CreateAccessControlService(),
            CreateRateLimitService(),
            CreateAutomaticBlockingService(),
            options,
            NullLogger<GatewayMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/immich";
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?id=1");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.True(proxyService.WasCalled);
        Assert.Equal("/immich", proxyService.LastRequest?.Path);
        Assert.Equal("?id=1", proxyService.LastRequest?.QueryString);
        Assert.Equal("198.51.100.1", proxyService.LastRequest?.ClientIp);
        Assert.Equal(200, context.Response.StatusCode);
    }

    private sealed class FakeProxyService : IProxyService
    {
        public bool WasCalled { get; private set; }
        public ProxyRequestContext? LastRequest { get; private set; }
        public string? LastUpstreamUrl { get; private set; }

        public Task<ProxyResponse> ForwardAsync(ProxyRequestContext request, string? upstreamUrl = null, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastRequest = request;
            LastUpstreamUrl = upstreamUrl;

            return Task.FromResult(new ProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase),
                Body = new MemoryStream()
            });
        }
    }

    private sealed class FakeClientIpResolver : IClientIpResolver
    {
        public ClientIpResolutionResult Result { get; set; } = new()
        {
            ClientIp = "127.0.0.1",
            ProxyChain = [],
            IsTrusted = true
        };

        public ClientIpResolutionResult Resolve(ClientIpContext context) => Result;
    }

    private static IApplicationPolicyService CreateApplicationPolicyService()
    {
        return new FakeApplicationPolicyService();
    }

    private static IAccessControlService CreateAccessControlService()
    {
        return new FakeAccessControlService();
    }

    private static IRateLimitService CreateRateLimitService()
    {
        return new FakeRateLimitService();
    }

    private static IAutomaticBlockingService CreateAutomaticBlockingService()
    {
        return new FakeAutomaticBlockingService();
    }

    private sealed class FakeApplicationPolicyService : IApplicationPolicyService
    {
        public Task<IReadOnlyList<Application.Applications.DTOs.ApplicationDto>> GetApplicationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Application.Applications.DTOs.ApplicationDto>>(Array.Empty<Application.Applications.DTOs.ApplicationDto>());

        public Task<Application.Applications.DTOs.ApplicationDto?> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Application.Applications.DTOs.ApplicationDto?>(null);

        public Task<Application.Applications.DTOs.ApplicationDto?> GetApplicationByDomainAsync(string domain, CancellationToken cancellationToken = default)
            => Task.FromResult<Application.Applications.DTOs.ApplicationDto?>(null);

        public Task<Application.Applications.DTOs.ApplicationDto> CreateApplicationAsync(Application.Applications.DTOs.CreateApplicationRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Application.Applications.DTOs.ApplicationDto> UpdateApplicationAsync(Guid id, Application.Applications.DTOs.UpdateApplicationRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeleteApplicationAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Application.Applications.DTOs.ApplicationPolicyDto?> GetPolicyAsync(Guid applicationId, CancellationToken cancellationToken = default)
            => Task.FromResult<Application.Applications.DTOs.ApplicationPolicyDto?>(null);

        public Task<Application.Applications.DTOs.ApplicationPolicyDto> UpdatePolicyAsync(Guid applicationId, Application.Applications.DTOs.UpdateApplicationPolicyRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Application.Applications.Models.ApplicationPolicyEvaluation> EvaluatePolicyAsync(Guid applicationId, string ipAddress, bool isAuthenticated, bool isIpTrusted, CancellationToken cancellationToken = default)
            => Task.FromResult(new Application.Applications.Models.ApplicationPolicyEvaluation
            {
                Allowed = true,
                Reason = null,
                RequiresAuthentication = false,
                IsAuthenticated = isAuthenticated
            });
    }

    private sealed class FakeAccessControlService : IAccessControlService
    {
        public Task<bool> IsIpTrustedAsync(string ipAddress, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> IsBlockedAsync(string ipAddress, Guid? deviceId, Guid? userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Application.AccessControl.Models.DeviceTrustResult> EvaluateDeviceTrustAsync(Guid userId, Guid deviceId, string ipAddress, CancellationToken cancellationToken = default)
            => Task.FromResult(new Application.AccessControl.Models.DeviceTrustResult
            {
                IsTrusted = true,
                IsPending = false,
                IsBlocked = false
            });
        public Task<IReadOnlyList<Application.AccessControl.DTOs.TrustedNetworkDto>> GetTrustedNetworksAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Application.AccessControl.DTOs.TrustedNetworkDto>>(Array.Empty<Application.AccessControl.DTOs.TrustedNetworkDto>());
        public Task<Application.AccessControl.DTOs.TrustedNetworkDto?> GetTrustedNetworkByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Application.AccessControl.DTOs.TrustedNetworkDto?>(null);
        public Task<Application.AccessControl.DTOs.TrustedNetworkDto> CreateTrustedNetworkAsync(Application.AccessControl.DTOs.CreateTrustedNetworkRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Application.AccessControl.DTOs.TrustedNetworkDto> UpdateTrustedNetworkAsync(Guid id, Application.AccessControl.DTOs.UpdateTrustedNetworkRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteTrustedNetworkAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Application.AccessControl.DTOs.BlocklistEntryDto>> GetBlocklistEntriesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Application.AccessControl.DTOs.BlocklistEntryDto>>(Array.Empty<Application.AccessControl.DTOs.BlocklistEntryDto>());
        public Task<Application.AccessControl.DTOs.BlocklistEntryDto?> GetBlocklistEntryByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Application.AccessControl.DTOs.BlocklistEntryDto?>(null);
        public Task<Application.AccessControl.DTOs.BlocklistEntryDto> CreateBlocklistEntryAsync(Application.AccessControl.DTOs.CreateBlocklistEntryRequest request, Guid? createdByUserId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Application.AccessControl.DTOs.BlocklistEntryDto> UpdateBlocklistEntryAsync(Guid id, Application.AccessControl.DTOs.CreateBlocklistEntryRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteBlocklistEntryAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Application.AccessControl.DTOs.AccessDecisionDto> ApproveDeviceAsync(Guid deviceId, Guid adminUserId, string? reason = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Application.AccessControl.DTOs.AccessDecisionDto> DenyDeviceAsync(Guid deviceId, Guid adminUserId, string? reason = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Application.AccessControl.DTOs.AccessDecisionDto>> GetDecisionsForTargetAsync(SecurityGateway.Domain.AccessControl.AccessDecisionType type, Guid targetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Application.AccessControl.DTOs.AccessDecisionDto>>(Array.Empty<Application.AccessControl.DTOs.AccessDecisionDto>());
    }

    private sealed class FakeRateLimitService : IRateLimitService
    {
        public Task<RateLimitResult> CheckAsync(Application.RateLimiting.Models.RateLimitRequestContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new RateLimitResult
            {
                Allowed = true,
                Remaining = int.MaxValue,
                ResetAt = DateTimeOffset.UtcNow
            });

        public Task<IReadOnlyList<Application.RateLimiting.DTOs.RateLimitRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Application.RateLimiting.DTOs.RateLimitRuleDto>>(Array.Empty<Application.RateLimiting.DTOs.RateLimitRuleDto>());

        public Task<Application.RateLimiting.DTOs.RateLimitRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Application.RateLimiting.DTOs.RateLimitRuleDto?>(null);

        public Task<Application.RateLimiting.DTOs.RateLimitRuleDto> CreateRuleAsync(Application.RateLimiting.DTOs.CreateRateLimitRuleRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Application.RateLimiting.DTOs.RateLimitRuleDto> UpdateRuleAsync(Guid id, Application.RateLimiting.DTOs.CreateRateLimitRuleRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_NoUpstreamConfigured_ReturnsBadGateway()
    {
        var proxyService = new FakeProxyService();
        var resolver = new FakeClientIpResolver
        {
            Result = new ClientIpResolutionResult
            {
                ClientIp = "198.51.100.1",
                ProxyChain = [],
                IsTrusted = true
            }
        };
        var options = new GatewayOptions { UpstreamNpmUrl = "" };

        var middleware = new GatewayMiddleware(
            _ => Task.CompletedTask,
            proxyService,
            resolver,
            null,
            CreateApplicationPolicyService(),
            CreateAccessControlService(),
            CreateRateLimitService(),
            CreateAutomaticBlockingService(),
            options,
            NullLogger<GatewayMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";
        context.Request.Host = new HostString("unknown.example.com");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
    }

    private sealed class FakeAutomaticBlockingService : IAutomaticBlockingService
    {
        public Task<BlockResultDto?> CheckAndBlockAsync(string ipAddress, int? threatScore = null, CancellationToken cancellationToken = default)
            => Task.FromResult<BlockResultDto?>(null);

        public Task<BlockResultDto> BlockAsync(string ipAddress, int? durationMinutes = null, string? reason = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new BlockResultDto { Blocked = true, IpAddress = ipAddress });

        public Task UnblockAsync(string ipAddress, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> IsBlockedAsync(string ipAddress, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
