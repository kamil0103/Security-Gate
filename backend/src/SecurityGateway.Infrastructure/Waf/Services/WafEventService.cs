using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.ThreatDetection.DTOs;
using SecurityGateway.Application.Waf;
using SecurityGateway.Application.Waf.DTOs;
using SecurityGateway.Domain.ThreatDetection;
using SecurityGateway.Domain.Waf;
using SecurityGateway.Application.Identity;

namespace SecurityGateway.Infrastructure.Waf.Services;

public sealed class WafEventService : IWafEventService
{
    private readonly IWafEventRepository _wafEventRepository;
    private readonly IAttackClassifier _attackClassifier;
    private readonly IIpAddressRepository _ipAddressRepository;
    private readonly IThreatDetectionService _threatDetectionService;
    private readonly IUnitOfWork _unitOfWork;

    public WafEventService(
        IWafEventRepository wafEventRepository,
        IAttackClassifier attackClassifier,
        IIpAddressRepository ipAddressRepository,
        IThreatDetectionService threatDetectionService,
        IUnitOfWork unitOfWork)
    {
        _wafEventRepository = wafEventRepository;
        _attackClassifier = attackClassifier;
        _ipAddressRepository = ipAddressRepository;
        _threatDetectionService = threatDetectionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<WafEventDto> IngestAsync(CreateWafEventRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceIp);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RuleId);

        var attackType = request.AttackType == AttackType.Unknown
            ? _attackClassifier.Classify(request.RuleId, request.RuleMessage)
            : request.AttackType;

        var severity = request.Severity == AttackSeverity.Info
            ? _attackClassifier.ClassifySeverity(request.RuleId, request.RuleMessage)
            : request.Severity;

        var wafEvent = new WafEvent
        {
            Timestamp = request.Timestamp,
            SourceIp = request.SourceIp,
            RequestId = request.RequestId,
            RuleId = request.RuleId,
            RuleMessage = request.RuleMessage,
            Severity = severity,
            AttackType = attackType,
            Method = request.Method,
            Uri = request.Uri,
            Host = request.Host,
            Action = request.Action,
            RawLog = request.RawLog
        };

        await _wafEventRepository.AddAsync(wafEvent, cancellationToken).ConfigureAwait(false);
        await CorrelateWithIpIntelligenceAsync(wafEvent, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await RecordSecurityEventAsync(wafEvent, cancellationToken).ConfigureAwait(false);

        return MapToDto(wafEvent);
    }

    public async Task<WafEventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wafEvent = await _wafEventRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return wafEvent is null ? null : MapToDto(wafEvent);
    }

    public async Task<IReadOnlyList<WafEventDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var events = await _wafEventRepository.GetRecentAsync(count, cancellationToken).ConfigureAwait(false);
        return events.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WafEventDto>> SearchAsync(WafEventFilter filter, CancellationToken cancellationToken = default)
    {
        var events = await _wafEventRepository.SearchAsync(
            filter.SourceIp,
            filter.AttackType,
            filter.Severity,
            filter.Action,
            filter.From,
            filter.To,
            filter.Skip,
            filter.Take,
            cancellationToken).ConfigureAwait(false);

        return events.Select(MapToDto).ToList().AsReadOnly();
    }

    private async Task RecordSecurityEventAsync(WafEvent wafEvent, CancellationToken cancellationToken)
    {
        if (wafEvent.Severity < AttackSeverity.High)
        {
            return;
        }

        try
        {
            await _threatDetectionService.RecordEventAsync(new CreateSecurityEventRequest
            {
                Type = SecurityEventType.WafEvent,
                Severity = wafEvent.Severity switch
                {
                    AttackSeverity.Critical => SecurityEventSeverity.Critical,
                    AttackSeverity.High => SecurityEventSeverity.High,
                    AttackSeverity.Medium => SecurityEventSeverity.Medium,
                    AttackSeverity.Low => SecurityEventSeverity.Low,
                    _ => SecurityEventSeverity.Info
                },
                SourceIp = wafEvent.SourceIp,
                Description = $"WAF {wafEvent.Action} {wafEvent.AttackType} attack: {wafEvent.RuleMessage}",
                RelatedEntityType = "WafEvent",
                RelatedEntityId = wafEvent.Id.ToString()
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort threat detection.
        }
    }

    private async Task CorrelateWithIpIntelligenceAsync(WafEvent wafEvent, CancellationToken cancellationToken)
    {
        var ip = await _ipAddressRepository.GetByIpAsync(wafEvent.SourceIp, cancellationToken).ConfigureAwait(false);

        if (ip is null)
        {
            return;
        }

        ip.AttackCount++;

        if (wafEvent.Severity >= AttackSeverity.High)
        {
            ip.ThreatScore = Math.Min(100, ip.ThreatScore + (int)wafEvent.Severity * 5);
            ip.ThreatLevel = ip.ThreatScore switch
            {
                >= 80 => "critical",
                >= 60 => "high",
                >= 40 => "medium",
                >= 20 => "low",
                _ => "info"
            };
        }

        await _ipAddressRepository.UpdateAsync(ip, cancellationToken).ConfigureAwait(false);
    }

    private static WafEventDto MapToDto(WafEvent wafEvent)
    {
        return new WafEventDto
        {
            Id = wafEvent.Id,
            Timestamp = wafEvent.Timestamp,
            SourceIp = wafEvent.SourceIp,
            RequestId = wafEvent.RequestId,
            RuleId = wafEvent.RuleId,
            RuleMessage = wafEvent.RuleMessage,
            Severity = wafEvent.Severity,
            AttackType = wafEvent.AttackType,
            Method = wafEvent.Method,
            Uri = wafEvent.Uri,
            Host = wafEvent.Host,
            Action = wafEvent.Action,
            ReceivedAt = wafEvent.ReceivedAt
        };
    }
}
