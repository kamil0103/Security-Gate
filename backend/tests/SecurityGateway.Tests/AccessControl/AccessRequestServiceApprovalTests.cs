using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.AccessControl.Models;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Applications.DTOs;
using SecurityGateway.Application.Applications.Models;
using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Blocking.DTOs;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Audit;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.IpIntelligence;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.AccessControl.Services;
using SecurityGateway.Infrastructure.Audit.Repositories;
using SecurityGateway.Infrastructure.Audit.Services;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using SecurityGateway.Tests.Helpers;
using SecurityGateway.Tests.TestHelpers;
using ApplicationEntity = SecurityGateway.Domain.Applications.Application;
using ApplicationPolicyEntity = SecurityGateway.Domain.Applications.ApplicationPolicy;
using Xunit;

namespace SecurityGateway.Tests.AccessControl;

public sealed class AccessRequestServiceApprovalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AccessRequestService _service;
    private readonly ApplicationEntity _application;
    private readonly ApplicationPolicyEntity _policy;
    private readonly User _adminUser;
    private readonly IpAddress _ipAddress;

    public AccessRequestServiceApprovalTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _application = new ApplicationEntity
        {
            Name = "Protected App",
            Domain = "protected.example.com",
            UpstreamUrl = "http://upstream",
            IsEnabled = true
        };
        _policy = new ApplicationPolicyEntity
        {
            Application = _application,
            RequireAuthentication = true,
            AllowAnonymousFromTrustedNetworks = false
        };
        _application.Policy = _policy;

        _adminUser = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            Role = UserRole.Administrator
        };

        _ipAddress = new IpAddress { Ip = "198.51.100.10" };

        _context.Applications.Add(_application);
        _context.Users.Add(_adminUser);
        _context.IpAddresses.Add(_ipAddress);
        _context.SaveChanges();

        var accessRequestRepository = new AccessRequestRepository(_context);
        var trustRecordRepository = new TrustRecordRepository(_context);
        var trustedNetworkRepository = new TrustedNetworkRepository(_context);
        var blocklistRepository = new BlocklistRepository(_context);
        var accessDecisionRepository = new AccessDecisionRepository(_context);
        var deviceRepository = new DeviceRepository(_context);
        var auditLogRepository = new AuditLogRepository(_context);
        var realAuditService = new AuditService(auditLogRepository, _context);

        var accessControlService = new AccessControlService(
            trustedNetworkRepository,
            blocklistRepository,
            accessDecisionRepository,
            deviceRepository,
            new FakeThreatDetectionService(),
            realAuditService,
            _context);

        var applicationPolicyService = new FakeApplicationPolicyServiceForApproval(_application, _policy);

        _service = new AccessRequestService(
            accessRequestRepository,
            trustRecordRepository,
            applicationPolicyService,
            accessControlService,
            new FakeAutomaticBlockingService(),
            deviceRepository,
            new FakeIpIntelligenceService(_ipAddress),
            realAuditService,
            new FakeNotificationDispatcher(),
            _context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task EvaluateAccessAsync_LoginWithSameSession_CreatesAuthenticatedChallenge()
    {
        const string fingerprint = "fp:login-handoff";
        const string session = "same-browser-session";
        var anonymous = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10", fingerprint: fingerprint, sessionId: session));
        Assert.Equal(AccessEvaluationDecision.Challenge, anonymous.Decision);
        Assert.Null(anonymous.AccessRequest!.UserId);
        _context.ChangeTracker.Clear();

        var authenticated = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10", fingerprint: fingerprint, sessionId: session,
            isAuthenticated: true, userId: _adminUser.Id));
        Assert.Equal(AccessEvaluationDecision.Challenge, authenticated.Decision);
        Assert.Equal(_adminUser.Id, authenticated.AccessRequest!.UserId);
        Assert.NotEqual(anonymous.PublicId, authenticated.PublicId);
        Assert.Equal(2, await _context.AccessRequests.CountAsync());
    }

    [Fact]
    public async Task EvaluateAccessAsync_RepeatedChallenge_ReusesWithoutCounterWrite()
    {
        var request = CreateContext(clientIp: "198.51.100.10",
            fingerprint: "fp:repeat", sessionId: "repeat-session");
        var first = await _service.EvaluateAccessAsync(request);
        _context.ChangeTracker.Clear();
        var second = await _service.EvaluateAccessAsync(request);
        Assert.Equal(first.PublicId, second.PublicId);
        Assert.Equal(1, (await _context.AccessRequests.SingleAsync()).RequestCount);
        Assert.False(_context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task GetStatusAsync_ExpiredPendingRequest_ReportsExpiredBeforeCleanupRuns()
    {
        var challenge = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10", fingerprint: "fp:status-expired",
            sessionId: "status-expired-session"));
        var stored = await _context.AccessRequests.SingleAsync(x => x.Id == challenge.AccessRequest!.Id);
        stored.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var status = await _service.GetStatusAsync(challenge.PublicId!);
        Assert.Equal("Expired", status.Status);
        Assert.Equal(AccessRequestStatus.Pending, (await _context.AccessRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task GetPendingAsync_ExcludesExpiredRequests()
    {
        var challenge = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: "fp:expired",
            sessionId: "expired-session"));

        Assert.Equal(AccessEvaluationDecision.Challenge, challenge.Decision);
        var pending = await _context.AccessRequests.SingleAsync(r => r.Id == challenge.AccessRequest!.Id);
        pending.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var repository = new AccessRequestRepository(_context);
        var visible = await repository.GetPendingAsync();

        Assert.DoesNotContain(visible, r => r.Id == challenge.AccessRequest!.Id);
    }

    [Theory]
    [InlineData(ApprovalScope.Device)]
    [InlineData(ApprovalScope.Permanent)]
    public async Task EvaluateAccessAsync_AfterDeviceOrPermanentApproval_NewSession_Allowed(ApprovalScope scope)
    {
        const string fingerprint = "fp:abc123";
        const string originalSessionId = "session-original";
        const string newSessionId = "session-new";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: originalSessionId, isAuthenticated: true, userId: _adminUser.Id));

        Assert.Equal(AccessEvaluationDecision.Challenge, first.Decision);
        var publicId = first.PublicId;
        Assert.NotNull(publicId);

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = scope,
            Reason = "Test approval"
        });

        var second = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: newSessionId, isAuthenticated: true, userId: _adminUser.Id));

        Assert.Equal(AccessEvaluationDecision.Allow, second.Decision);
        Assert.Equal(1, _context.AccessRequests.Count());
    }

    [Fact]
    public async Task EvaluateAccessAsync_AfterDeviceApproval_ChangedIp_Allowed()
    {
        const string fingerprint = "fp:abc123";
        const string sessionId = "session-1";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Device,
            Reason = "Test approval"
        });

        var second = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.99",
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        Assert.Equal(AccessEvaluationDecision.Allow, second.Decision);
    }

    [Fact]
    public async Task EvaluateAccessAsync_AfterSessionApproval_NewSession_Challenged()
    {
        const string fingerprint = "fp:abc123";
        const string originalSessionId = "session-original";
        const string newSessionId = "session-new";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: originalSessionId, isAuthenticated: true, userId: _adminUser.Id));

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Session,
            Reason = "Test approval"
        });

        var second = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: newSessionId, isAuthenticated: true, userId: _adminUser.Id));

        Assert.Equal(AccessEvaluationDecision.Challenge, second.Decision);
    }

    [Fact]
    public async Task EvaluateAccessAsync_AfterIpApproval_DifferentIp_Challenged()
    {
        const string fingerprint = "fp:abc123";
        const string sessionId = "session-1";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Ip,
            Reason = "Test approval"
        });

        var second = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.99",
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        Assert.Equal(AccessEvaluationDecision.Challenge, second.Decision);
    }

    [Fact]
    public async Task EvaluateAccessAsync_DifferentFingerprint_AfterDeviceApproval_Challenged()
    {
        const string fingerprint = "fp:abc123";
        const string otherFingerprint = "fp:other";
        const string sessionId = "session-1";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Device,
            Reason = "Test approval"
        });

        var second = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: otherFingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        Assert.Equal(AccessEvaluationDecision.Challenge, second.Decision);
    }

    [Fact]
    public async Task EvaluateAccessAsync_AfterApproval_RefreshManyTimes_NoNewRequests()
    {
        const string fingerprint = "fp:abc123";
        const string sessionId = "session-1";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Device,
            Reason = "Test approval"
        });

        for (var i = 0; i < 20; i++)
        {
            var result = await _service.EvaluateAccessAsync(CreateContext(
                clientIp: "198.51.100.10",
                fingerprint: fingerprint,
                sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

            Assert.Equal(AccessEvaluationDecision.Allow, result.Decision);
        }

        Assert.Equal(1, _context.AccessRequests.Count());
    }

    [Fact]
    public async Task EvaluateAccessAsync_ConcurrentRequestsAfterApproval_NoDuplicatesOrExceptions()
    {
        const string fingerprint = "fp:abc123";
        const string sessionId = "session-1";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Device,
            Reason = "Test approval"
        });

        _context.ChangeTracker.Clear();

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _service.EvaluateAccessAsync(CreateContext(
                clientIp: "198.51.100.10",
                fingerprint: fingerprint,
                sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(AccessEvaluationDecision.Allow, r.Decision));
        Assert.Equal(1, _context.AccessRequests.Count());
    }

    [Fact]
    public async Task ResolveAsync_Approval_CreatesTrustRecordWithScopeAwareIdentity()
    {
        const string fingerprint = "fp:abc123";
        const string sessionId = "session-1";
        const string clientIp = "198.51.100.10";

        var first = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: clientIp,
            fingerprint: fingerprint,
            sessionId: sessionId, isAuthenticated: true, userId: _adminUser.Id));

        _context.ChangeTracker.Clear();

        await _service.ResolveAsync(first.AccessRequest!.Id, _adminUser.Id, new ResolveAccessRequestRequest
        {
            Decision = AccessRequestDecision.Approve,
            ApprovalScope = ApprovalScope.Permanent,
            Reason = "Test approval"
        });

        var trustRecord = _context.TrustRecords.Single();
        Assert.Equal(TrustScope.Permanent, trustRecord.Scope);
        Assert.Null(trustRecord.ClientIp);
        Assert.Null(trustRecord.SessionId);
        Assert.Equal(fingerprint, trustRecord.DeviceFingerprint);
    }

    [Fact]
    public async Task ResolveAsync_AnonymousChallenge_ApprovalRejectedWithoutTrustRecord()
    {
        var challenge = await _service.EvaluateAccessAsync(CreateContext(
            clientIp: "198.51.100.10",
            fingerprint: "fp:anonymous",
            sessionId: "anonymous-session"));

        Assert.Equal(AccessEvaluationDecision.Challenge, challenge.Decision);
        _context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ResolveAsync(
            challenge.AccessRequest!.Id,
            _adminUser.Id,
            new ResolveAccessRequestRequest
            {
                Decision = AccessRequestDecision.Approve,
                ApprovalScope = ApprovalScope.Device
            }));

        Assert.Empty(_context.TrustRecords);
        Assert.Equal(AccessRequestStatus.Pending, _context.AccessRequests.Single(r => r.Id == challenge.AccessRequest!.Id).Status);
    }

    private AccessEvaluationContext CreateContext(
        string clientIp,
        string fingerprint,
        string sessionId,
        bool isAuthenticated = false,
        Guid? userId = null)
    {
        return new AccessEvaluationContext
        {
            ApplicationId = _application.Id,
            ClientIp = clientIp,
            UserAgent = "TestAgent/1.0",
            DeviceFingerprint = fingerprint,
            DeviceName = "Test Device",
            DeviceId = null,
            SessionId = sessionId,
            UserId = userId,
            Username = userId.HasValue ? "user" : null,
            HttpMethod = "GET",
            RequestedPath = "/",
            QueryString = null,
            IsAuthenticated = isAuthenticated,
            CloudflareCountry = null
        };
    }

    private sealed class FakeApplicationPolicyServiceForApproval : IApplicationPolicyService
    {
        private readonly ApplicationEntity _application;
        private readonly ApplicationPolicyEntity _policy;

        public FakeApplicationPolicyServiceForApproval(ApplicationEntity application, ApplicationPolicyEntity policy)
        {
            _application = application;
            _policy = policy;
        }

        public Task<IReadOnlyList<ApplicationDto>> GetApplicationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ApplicationDto>>(Array.Empty<ApplicationDto>());

        public Task<ApplicationDto?> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ApplicationDto?>(id == _application.Id ? MapApplication() : null);

        public Task<ApplicationDto?> GetApplicationByDomainAsync(string domain, CancellationToken cancellationToken = default)
            => Task.FromResult<ApplicationDto?>(domain == _application.Domain ? MapApplication() : null);

        public Task<ApplicationDto> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ApplicationDto> UpdateApplicationAsync(Guid id, UpdateApplicationRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeleteApplicationAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ApplicationPolicyDto?> GetPolicyAsync(Guid applicationId, CancellationToken cancellationToken = default)
            => Task.FromResult<ApplicationPolicyDto?>(MapPolicy());

        public Task<ApplicationPolicyDto> UpdatePolicyAsync(Guid applicationId, UpdateApplicationPolicyRequest request, CancellationToken cancellationToken = default, Guid? adminUserId = null)
            => throw new NotImplementedException();

        public Task<ApplicationPolicyEvaluation> EvaluatePolicyAsync(Guid applicationId, string ipAddress, bool isAuthenticated, bool isIpTrusted, string? cloudflareCountry = null, string? path = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ApplicationPolicyEvaluation
            {
                Allowed = false,
                Reason = "Authentication required.",
                RequiresAuthentication = true,
                IsAuthenticated = isAuthenticated
            });
        }

        private ApplicationDto MapApplication()
        {
            return new ApplicationDto
            {
                Id = _application.Id,
                Name = _application.Name,
                Domain = _application.Domain,
                UpstreamUrl = _application.UpstreamUrl,
                IsEnabled = _application.IsEnabled,
                CreatedAt = _application.CreatedAt,
                Policy = MapPolicy()
            };
        }

        private ApplicationPolicyDto MapPolicy()
        {
            return new ApplicationPolicyDto
            {
                Id = _policy.Id,
                ApplicationId = _application.Id,
                RequireAuthentication = _policy.RequireAuthentication,
                AllowAnonymousFromTrustedNetworks = _policy.AllowAnonymousFromTrustedNetworks,
                AllowedCountries = _policy.AllowedCountries,
                BlockedCountries = _policy.BlockedCountries,
                AllowedIpAddresses = _policy.AllowedIpAddresses,
                BlockedIpAddresses = _policy.BlockedIpAddresses,
                AllowedCloudflareCountries = _policy.AllowedCloudflareCountries,
                BlockedCloudflareCountries = _policy.BlockedCloudflareCountries,
                BypassAuthenticationPaths = _policy.BypassAuthenticationPaths
            };
        }
    }

    private sealed class FakeIpIntelligenceService : IIpIntelligenceService
    {
        private readonly IpAddress _ip;

        public FakeIpIntelligenceService(IpAddress ip)
        {
            _ip = ip;
        }

        public Task<IpAddressDto> TrackAsync(TrackIpRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new IpAddressDto { Id = _ip.Id, Ip = request.IpAddress });

        public Task<IpAddressDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<IpAddressDto?>(null);

        public Task<IReadOnlyList<IpAddressDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IpAddressDto>>(Array.Empty<IpAddressDto>());
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

    private sealed class FakeNotificationDispatcher : INotificationDispatcher
    {
        public Task DispatchAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
