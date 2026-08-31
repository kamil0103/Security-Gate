using SecurityGateway.Application.Blocking;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Application.ThreatDetection.DTOs;
using SecurityGateway.Application.ThreatDetection.Models;
using SecurityGateway.Domain.IpIntelligence;
using SecurityGateway.Domain.ThreatDetection;

namespace SecurityGateway.Infrastructure.ThreatDetection.Services;

public sealed class ThreatDetectionService : IThreatDetectionService
{
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly IThreatScoreRuleRepository _ruleRepository;
    private readonly IIpAddressRepository _ipAddressRepository;
    private readonly IAutomaticBlockingService? _automaticBlockingService;
    private readonly IUnitOfWork _unitOfWork;

    public ThreatDetectionService(
        ISecurityEventRepository securityEventRepository,
        IThreatScoreRuleRepository ruleRepository,
        IIpAddressRepository ipAddressRepository,
        IUnitOfWork unitOfWork,
        IAutomaticBlockingService? automaticBlockingService = null)
    {
        _securityEventRepository = securityEventRepository;
        _ruleRepository = ruleRepository;
        _ipAddressRepository = ipAddressRepository;
        _automaticBlockingService = automaticBlockingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityEventDto> RecordEventAsync(CreateSecurityEventRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceIp);

        var securityEvent = new SecurityEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = request.Type,
            Severity = request.Severity,
            SourceIp = request.SourceIp,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            Description = request.Description,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId
        };

        await _securityEventRepository.AddAsync(securityEvent, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var scoreResult = await EvaluateThreatScoreAsync(request.SourceIp, cancellationToken).ConfigureAwait(false);

        if (scoreResult is { Escalated: true })
        {
            securityEvent.Description = $"{securityEvent.Description} [Threat score escalated to {scoreResult.NewScore}]";
        }

        return MapEvent(securityEvent);
    }

    public async Task<SecurityEventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var securityEvent = await _securityEventRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return securityEvent is null ? null : MapEvent(securityEvent);
    }

    public async Task<IReadOnlyList<SecurityEventDto>> GetRecentEventsAsync(int count, CancellationToken cancellationToken = default)
    {
        var events = await _securityEventRepository.GetRecentAsync(count, cancellationToken).ConfigureAwait(false);
        return events.Select(MapEvent).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<SecurityEventDto>> SearchEventsAsync(SecurityEventFilter filter, CancellationToken cancellationToken = default)
    {
        var events = await _securityEventRepository.SearchAsync(
            filter.Type,
            filter.Severity,
            filter.SourceIp,
            filter.UserId,
            filter.From,
            filter.To,
            filter.Skip,
            filter.Take,
            cancellationToken).ConfigureAwait(false);

        return events.Select(MapEvent).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ThreatScoreRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return rules.Select(MapRule).ToList().AsReadOnly();
    }

    public async Task<ThreatScoreRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return rule is null ? null : MapRule(rule);
    }

    public async Task<ThreatScoreRuleDto> CreateRuleAsync(CreateThreatScoreRuleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRuleRequest(request);

        var rule = new ThreatScoreRule
        {
            Name = request.Name,
            EventType = request.EventType,
            EventCountThreshold = request.EventCountThreshold,
            TimeWindowSeconds = request.TimeWindowSeconds,
            ScoreImpact = request.ScoreImpact,
            Severity = request.Severity
        };

        await _ruleRepository.AddAsync(rule, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapRule(rule);
    }

    public async Task<ThreatScoreRuleDto> UpdateRuleAsync(Guid id, CreateThreatScoreRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Threat score rule not found.");

        ValidateRuleRequest(request);

        rule.Name = request.Name;
        rule.EventType = request.EventType;
        rule.EventCountThreshold = request.EventCountThreshold;
        rule.TimeWindowSeconds = request.TimeWindowSeconds;
        rule.ScoreImpact = request.ScoreImpact;
        rule.Severity = request.Severity;

        await _ruleRepository.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapRule(rule);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Threat score rule not found.");

        await _ruleRepository.DeleteAsync(rule, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ThreatScoreResult?> EvaluateThreatScoreAsync(string sourceIp, CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetEnabledAsync(cancellationToken).ConfigureAwait(false);

        if (rules.Count == 0)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var maxScoreImpact = 0;
        var escalated = false;
        var reasons = new List<string>();

        foreach (var rule in rules)
        {
            var from = now.AddSeconds(-rule.TimeWindowSeconds);
            var count = await _securityEventRepository.CountEventsAsync(sourceIp, rule.EventType, from, cancellationToken).ConfigureAwait(false);

            if (count >= rule.EventCountThreshold)
            {
                maxScoreImpact = Math.Max(maxScoreImpact, rule.ScoreImpact);
                reasons.Add($"{rule.EventType}: {count} events (threshold {rule.EventCountThreshold})");
            }
        }

        if (maxScoreImpact == 0)
        {
            return null;
        }

        var ip = await _ipAddressRepository.GetByIpAsync(sourceIp, cancellationToken).ConfigureAwait(false);

        if (ip is null)
        {
            ip = new IpAddress
            {
                Ip = sourceIp,
                ThreatScore = Math.Min(100, maxScoreImpact),
                ThreatLevel = ScoreToLevel(Math.Min(100, maxScoreImpact))
            };

            await _ipAddressRepository.AddAsync(ip, cancellationToken).ConfigureAwait(false);
            escalated = true;
        }
        else
        {
            var newScore = Math.Min(100, ip.ThreatScore + maxScoreImpact);

            if (newScore > ip.ThreatScore)
            {
                ip.ThreatScore = newScore;
                ip.ThreatLevel = ScoreToLevel(newScore);
                escalated = true;
            }

            await _ipAddressRepository.UpdateAsync(ip, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_automaticBlockingService is not null && escalated)
        {
            await _automaticBlockingService.CheckAndBlockAsync(sourceIp, ip.ThreatScore, cancellationToken).ConfigureAwait(false);
        }

        return new ThreatScoreResult
        {
            SourceIp = sourceIp,
            NewScore = ip.ThreatScore,
            ThreatLevel = ip.ThreatLevel ?? ScoreToLevel(ip.ThreatScore),
            Escalated = escalated,
            Reason = string.Join("; ", reasons)
        };
    }

    private static string ScoreToLevel(int score)
    {
        return score switch
        {
            >= 80 => "critical",
            >= 60 => "high",
            >= 40 => "medium",
            >= 20 => "low",
            _ => "info"
        };
    }

    private static void ValidateRuleRequest(CreateThreatScoreRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        if (request.EventCountThreshold <= 0)
        {
            throw new ArgumentException("Event count threshold must be greater than zero.", nameof(request));
        }

        if (request.TimeWindowSeconds <= 0)
        {
            throw new ArgumentException("Time window seconds must be greater than zero.", nameof(request));
        }

        if (request.ScoreImpact <= 0 || request.ScoreImpact > 100)
        {
            throw new ArgumentException("Score impact must be between 1 and 100.", nameof(request));
        }
    }

    private static SecurityEventDto MapEvent(SecurityEvent securityEvent)
    {
        return new SecurityEventDto
        {
            Id = securityEvent.Id,
            Timestamp = securityEvent.Timestamp,
            Type = securityEvent.Type,
            Severity = securityEvent.Severity,
            SourceIp = securityEvent.SourceIp,
            UserId = securityEvent.UserId,
            DeviceId = securityEvent.DeviceId,
            Description = securityEvent.Description,
            RelatedEntityType = securityEvent.RelatedEntityType,
            RelatedEntityId = securityEvent.RelatedEntityId,
            CreatedAt = securityEvent.CreatedAt
        };
    }

    private static ThreatScoreRuleDto MapRule(ThreatScoreRule rule)
    {
        return new ThreatScoreRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            EventType = rule.EventType,
            EventCountThreshold = rule.EventCountThreshold,
            TimeWindowSeconds = rule.TimeWindowSeconds,
            ScoreImpact = rule.ScoreImpact,
            Severity = rule.Severity,
            IsEnabled = rule.IsEnabled,
            CreatedAt = rule.CreatedAt
        };
    }
}
