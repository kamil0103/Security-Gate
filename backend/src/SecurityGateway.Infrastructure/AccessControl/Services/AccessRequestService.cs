using System.Security.Cryptography;
using System.Text;
using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.AccessControl.DTOs;
using SecurityGateway.Application.AccessControl.Models;
using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Applications.DTOs;
using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.Notifications;
using SecurityGateway.Application.Notifications.DTOs;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.Audit;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.Notifications;
using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Infrastructure.AccessControl.Services;

public sealed class AccessRequestService : IAccessRequestService
{
    private readonly IAccessRequestRepository _accessRequestRepository;
    private readonly ITrustRecordRepository _trustRecordRepository;
    private readonly IApplicationPolicyService _applicationPolicyService;
    private readonly IAccessControlService _accessControlService;
    private readonly IAutomaticBlockingService _automaticBlockingService;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IIpIntelligenceService _ipIntelligenceService;
    private readonly IAuditService _auditService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;

    public AccessRequestService(
        IAccessRequestRepository accessRequestRepository,
        ITrustRecordRepository trustRecordRepository,
        IApplicationPolicyService applicationPolicyService,
        IAccessControlService accessControlService,
        IAutomaticBlockingService automaticBlockingService,
        IDeviceRepository deviceRepository,
        IIpIntelligenceService ipIntelligenceService,
        IAuditService auditService,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork)
    {
        _accessRequestRepository = accessRequestRepository;
        _trustRecordRepository = trustRecordRepository;
        _applicationPolicyService = applicationPolicyService;
        _accessControlService = accessControlService;
        _automaticBlockingService = automaticBlockingService;
        _deviceRepository = deviceRepository;
        _ipIntelligenceService = ipIntelligenceService;
        _auditService = auditService;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccessEvaluationResult> EvaluateAccessAsync(AccessEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var application = await _applicationPolicyService.GetApplicationByIdAsync(context.ApplicationId, cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Deny("Application not found.");
        }

        if (!application.IsEnabled)
        {
            return Deny("Application is disabled.");
        }

        var deviceId = await ResolveDeviceIdAsync(context, cancellationToken).ConfigureAwait(false);

        var blockResult = await _accessControlService.IsBlockedAsync(context.ClientIp, deviceId, context.UserId, cancellationToken).ConfigureAwait(false);
        if (blockResult)
        {
            return Block("Access blocked by policy.");
        }

        var autoBlock = await _automaticBlockingService.CheckAndBlockAsync(context.ClientIp, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (autoBlock is { Blocked: true })
        {
            return Block($"Access blocked: {autoBlock.Reason}");
        }

        var existingTrust = await _trustRecordRepository.FindActiveAsync(
            context.ApplicationId,
            context.ClientIp,
            context.DeviceFingerprint,
            context.UserId,
            context.SessionId,
            cancellationToken).ConfigureAwait(false);

        if (existingTrust.Count > 0)
        {
            return Allow();
        }

        if (context.IsAuthenticated && context.UserId.HasValue && !string.IsNullOrWhiteSpace(context.DeviceFingerprint))
        {
            var device = await _deviceRepository.GetByUserAndFingerprintAsync(context.UserId.Value, context.DeviceFingerprint, cancellationToken).ConfigureAwait(false);
            if (device is not null)
            {
                if (device.TrustStatus == DeviceTrustStatus.Blocked)
                {
                    return Block("Device is blocked.");
                }

                if (device.TrustStatus == DeviceTrustStatus.Pending)
                {
                    return await ChallengeAsync(context, application, "Device pending approval", deviceId, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                return await ChallengeAsync(context, application, "Unknown device for authenticated user", deviceId, cancellationToken).ConfigureAwait(false);
            }
        }

        var isIpTrusted = await _accessControlService.IsIpTrustedAsync(context.ClientIp, cancellationToken).ConfigureAwait(false);
        var policyEval = await _applicationPolicyService.EvaluatePolicyAsync(
            context.ApplicationId,
            context.ClientIp,
            context.IsAuthenticated,
            isIpTrusted,
            context.CloudflareCountry,
            context.RequestedPath,
            cancellationToken).ConfigureAwait(false);

        if (!policyEval.Allowed)
        {
            if (policyEval.RequiresAuthentication)
            {
                return await ChallengeAsync(context, application, policyEval.Reason ?? "Authentication required.", deviceId, cancellationToken).ConfigureAwait(false);
            }

            return Deny(policyEval.Reason ?? "Access denied by policy.");
        }

        if (context.IsAuthenticated && context.UserId.HasValue && string.IsNullOrWhiteSpace(context.DeviceFingerprint))
        {
            return await ChallengeAsync(context, application, "Unrecognized device session", deviceId, cancellationToken).ConfigureAwait(false);
        }

        if (!context.IsAuthenticated && application.Policy?.RequireAuthentication != false)
        {
            return await ChallengeAsync(context, application, "Authentication required.", deviceId, cancellationToken).ConfigureAwait(false);
        }

        return Allow();
    }

    public Task<AccessRequestDto?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        return MapAsync(_accessRequestRepository.GetByPublicIdAsync(publicId, cancellationToken), cancellationToken);
    }

    public async Task<AccessRequestStatusDto> GetStatusAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var request = await _accessRequestRepository.GetByPublicIdAsync(publicId, cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            throw new InvalidOperationException("Access request not found.");
        }

        return new AccessRequestStatusDto
        {
            PublicId = request.PublicId,
            Status = request.Status.ToString(),
            ExpiresAt = request.ExpiresAt,
            Reason = request.ResolutionReason
        };
    }

    public async Task<IReadOnlyList<AccessRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _accessRequestRepository.GetPendingAsync(cancellationToken).ConfigureAwait(false);
        return requests.Select(Map).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<AccessRequestDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var requests = await _accessRequestRepository.GetRecentAsync(count, cancellationToken).ConfigureAwait(false);
        return requests.Select(Map).ToList().AsReadOnly();
    }

    public async Task<AccessRequestDto> ResolveAsync(Guid accessRequestId, Guid adminUserId, ResolveAccessRequestRequest request, CancellationToken cancellationToken = default)
    {
        var accessRequest = await _accessRequestRepository.GetByIdAsync(accessRequestId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Access request not found.");

        if (accessRequest.Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException("Access request is no longer pending.");
        }

        if (accessRequest.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            accessRequest.Status = AccessRequestStatus.Expired;
            accessRequest.ResolvedAt = DateTimeOffset.UtcNow;
            await _accessRequestRepository.UpdateAsync(accessRequest, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("Access request has expired.");
        }

        accessRequest.Decision = request.Decision;
        accessRequest.ApprovalScope = request.ApprovalScope;
        accessRequest.ResolutionReason = request.Reason;
        accessRequest.ReviewedByUserId = adminUserId;
        accessRequest.ResolvedAt = DateTimeOffset.UtcNow;
        accessRequest.UpdatedAt = DateTimeOffset.UtcNow;

        switch (request.Decision)
        {
            case AccessRequestDecision.Approve:
                accessRequest.Status = AccessRequestStatus.Approved;
                await CreateTrustRecordAsync(accessRequest, cancellationToken).ConfigureAwait(false);
                break;
            case AccessRequestDecision.Deny:
                accessRequest.Status = AccessRequestStatus.Denied;
                break;
            case AccessRequestDecision.BlockIp:
                accessRequest.Status = AccessRequestStatus.Denied;
                await _accessControlService.CreateBlocklistEntryAsync(
                    new CreateBlocklistEntryRequest
                    {
                        Type = BlocklistEntryType.Ip,
                        Value = accessRequest.ClientIp,
                        Reason = request.Reason ?? "Blocked from access request"
                    },
                    adminUserId,
                    cancellationToken).ConfigureAwait(false);
                break;
            case AccessRequestDecision.BlockDevice:
                accessRequest.Status = AccessRequestStatus.Denied;
                if (!string.IsNullOrWhiteSpace(accessRequest.DeviceFingerprint))
                {
                    await _accessControlService.CreateBlocklistEntryAsync(
                        new CreateBlocklistEntryRequest
                        {
                            Type = BlocklistEntryType.Device,
                            Value = accessRequest.DeviceFingerprint,
                            Reason = request.Reason ?? "Blocked from access request"
                        },
                        adminUserId,
                        cancellationToken).ConfigureAwait(false);
                }
                break;
        }

        await _accessRequestRepository.UpdateAsync(accessRequest, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.AccessControl,
            $"AccessRequest{request.Decision}",
            adminUserId,
            null,
            accessRequest.ClientIp,
            $"Access request {accessRequest.PublicId} for {accessRequest.Application.Domain} was {request.Decision}. Reason: {request.Reason}",
            true,
            cancellationToken).ConfigureAwait(false);

        return Map(accessRequest);
    }

    public async Task RevokeTrustAsync(Guid trustRecordId, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var record = await _trustRecordRepository.FindActiveAsync(
            Guid.Empty,
            string.Empty,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        // Placeholder: repository does not expose single-record lookup by ID yet.
        // This will be implemented when the trust UI is built.
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task<Guid?> ResolveDeviceIdAsync(AccessEvaluationContext context, CancellationToken cancellationToken)
    {
        if (!context.UserId.HasValue || string.IsNullOrWhiteSpace(context.DeviceFingerprint))
        {
            return null;
        }

        var device = await _deviceRepository.GetByUserAndFingerprintAsync(context.UserId.Value, context.DeviceFingerprint, cancellationToken).ConfigureAwait(false);
        return device?.Id;
    }

    private async Task<AccessEvaluationResult> ChallengeAsync(
        AccessEvaluationContext context,
        ApplicationDto? application,
        string reason,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (application is null)
        {
            return Deny("Application not found.");
        }

        var existing = await _accessRequestRepository.FindPendingAsync(
            application.Id,
            context.ClientIp,
            context.DeviceFingerprint,
            context.SessionId,
            cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.RequestCount++;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await _accessRequestRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new AccessEvaluationResult
            {
                Decision = AccessEvaluationDecision.Challenge,
                Reason = existing.ReasonForChallenge,
                AccessRequest = existing,
                PublicId = existing.PublicId
            };
        }

        var ipInfo = await _ipIntelligenceService.TrackAsync(
            new TrackIpRequest
            {
                IpAddress = context.ClientIp,
                UserId = context.UserId,
                DeviceId = deviceId
            },
            cancellationToken).ConfigureAwait(false);

        var request = new AccessRequest
        {
            ApplicationId = application.Id,
            ApplicationPolicyId = application.Policy?.Id,
            HttpMethod = context.HttpMethod,
            RequestedPath = context.RequestedPath,
            QueryString = context.QueryString,
            ClientIp = context.ClientIp,
            IpAddressId = ipInfo.Id,
            UserId = context.UserId,
            Username = context.Username,
            DeviceFingerprint = context.DeviceFingerprint,
            DeviceName = context.DeviceName,
            DeviceId = context.DeviceId,
            SessionId = context.SessionId,
            UserAgent = context.UserAgent,
            Browser = context.Browser,
            OperatingSystem = context.OperatingSystem,
            Country = ipInfo.Country,
            CountryCode = ipInfo.CountryCode,
            Region = ipInfo.Region,
            City = ipInfo.City,
            Asn = ipInfo.Asn,
            Isp = ipInfo.Isp,
            IsVpn = ipInfo.IsVpn,
            IsProxy = ipInfo.IsProxy,
            IsTor = ipInfo.IsTor,
            IsDatacenter = ipInfo.IsDatacenter,
            ThreatScore = ipInfo.ThreatScore,
            ThreatLevel = ipInfo.ThreatLevel,
            ReasonForChallenge = reason,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await _accessRequestRepository.AddAsync(request, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.AccessControl,
            "AccessRequestCreated",
            context.UserId,
            context.Username,
            context.ClientIp,
            $"Access request {request.PublicId} created for {application.Domain}. Reason: {reason}",
            true,
            cancellationToken).ConfigureAwait(false);

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage
            {
                Title = "New Access Request",
                Body = $"Application: {application.Name}\nIP: {context.ClientIp}\nCountry: {ipInfo.Country ?? "Unknown"}\nDevice: {context.Browser ?? "Unknown"} / {context.OperatingSystem ?? "Unknown"}\nThreat: {ipInfo.ThreatLevel ?? "Unknown"}\nReason: {reason}",
                Severity = SecurityEventSeverity.Info,
                SourceIp = context.ClientIp,
                EventType = "AccessRequestCreated",
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken).ConfigureAwait(false);

        return new AccessEvaluationResult
        {
            Decision = AccessEvaluationDecision.Challenge,
            Reason = reason,
            AccessRequest = request,
            PublicId = request.PublicId
        };
    }

    private async Task CreateTrustRecordAsync(AccessRequest request, CancellationToken cancellationToken)
    {
        var scope = request.ApprovalScope switch
        {
            ApprovalScope.Session => TrustScope.Session,
            ApprovalScope.Device => TrustScope.Device,
            ApprovalScope.IpAndDevice => TrustScope.IpAndDevice,
            ApprovalScope.Ip => TrustScope.Ip,
            ApprovalScope.Permanent => TrustScope.Permanent,
            ApprovalScope.Once or null => TrustScope.Session,
            _ => TrustScope.Session
        };

        var expiresAt = scope switch
        {
            TrustScope.Session => DateTimeOffset.UtcNow.AddHours(1),
            TrustScope.Ip => DateTimeOffset.UtcNow.AddDays(1),
            TrustScope.Device => DateTimeOffset.UtcNow.AddDays(30),
            TrustScope.IpAndDevice => DateTimeOffset.UtcNow.AddDays(7),
            TrustScope.Permanent => (DateTimeOffset?)null,
            _ => DateTimeOffset.UtcNow.AddHours(1)
        };

        var trust = new TrustRecord
        {
            Scope = scope,
            ApplicationId = request.ApplicationId,
            ClientIp = request.ClientIp,
            DeviceFingerprint = request.DeviceFingerprint,
            UserId = request.UserId,
            SessionId = request.SessionId,
            ExpiresAt = expiresAt,
            AccessRequestId = request.Id,
            CreatedByUserId = request.ReviewedByUserId
        };

        await _trustRecordRepository.AddAsync(trust, cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync(
            AuditCategory.AccessControl,
            "TrustRecordCreated",
            request.ReviewedByUserId,
            null,
            request.ClientIp,
            $"Trust record created for {request.Application.Domain} with scope {scope}",
            true,
            cancellationToken).ConfigureAwait(false);
    }

    private static AccessEvaluationResult Allow() => new() { Decision = AccessEvaluationDecision.Allow };
    private static AccessEvaluationResult Deny(string reason) => new() { Decision = AccessEvaluationDecision.Deny, Reason = reason };
    private static AccessEvaluationResult Block(string reason) => new() { Decision = AccessEvaluationDecision.Block, Reason = reason };

    private static AccessRequestDto Map(AccessRequest r)
    {
        return new AccessRequestDto
        {
            Id = r.Id,
            PublicId = r.PublicId,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            ExpiresAt = r.ExpiresAt,
            ResolvedAt = r.ResolvedAt,
            ResolutionReason = r.ResolutionReason,
            ApplicationId = r.ApplicationId,
            ApplicationName = r.Application.Name,
            ApplicationDomain = r.Application.Domain,
            HttpMethod = r.HttpMethod,
            RequestedPath = r.RequestedPath,
            ClientIp = r.ClientIp,
            Country = r.Country,
            CountryCode = r.CountryCode,
            Region = r.Region,
            City = r.City,
            Isp = r.Isp,
            Asn = r.Asn,
            IsVpn = r.IsVpn,
            IsProxy = r.IsProxy,
            IsTor = r.IsTor,
            IsDatacenter = r.IsDatacenter,
            ThreatScore = r.ThreatScore,
            ThreatLevel = r.ThreatLevel,
            RequestCount = r.RequestCount,
            DeviceFingerprint = r.DeviceFingerprint,
            DeviceName = r.DeviceName,
            DeviceId = r.DeviceId,
            SessionId = r.SessionId,
            UserAgent = r.UserAgent,
            Browser = r.Browser,
            OperatingSystem = r.OperatingSystem,
            UserId = r.UserId,
            Username = r.Username,
            ReasonForChallenge = r.ReasonForChallenge,
            ReviewedByUserId = r.ReviewedByUserId,
            ReviewedByUsername = r.ReviewedByUser?.Username,
            Decision = r.Decision?.ToString(),
            ApprovalScope = r.ApprovalScope?.ToString()
        };
    }

    private async Task<AccessRequestDto?> MapAsync(Task<AccessRequest?> task, CancellationToken cancellationToken)
    {
        var r = await task.ConfigureAwait(false);
        return r is null ? null : Map(r);
    }
}
