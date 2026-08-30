using SecurityGateway.Application.AccessControl;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.RateLimiting;
using SecurityGateway.Application.RateLimiting.DTOs;
using SecurityGateway.Application.RateLimiting.Models;
using SecurityGateway.Domain.AccessControl;
using SecurityGateway.Domain.RateLimiting;

namespace SecurityGateway.Infrastructure.RateLimiting.Services;

public sealed class RateLimitService : IRateLimitService
{
    private readonly IRateLimitStore _rateLimitStore;
    private readonly IRateLimitRuleRepository _ruleRepository;
    private readonly IBlocklistRepository _blocklistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RateLimitService(
        IRateLimitStore rateLimitStore,
        IRateLimitRuleRepository ruleRepository,
        IBlocklistRepository blocklistRepository,
        IUnitOfWork unitOfWork)
    {
        _rateLimitStore = rateLimitStore;
        _ruleRepository = ruleRepository;
        _blocklistRepository = blocklistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RateLimitResult> CheckAsync(RateLimitRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!_rateLimitStore.IsAvailable)
        {
            return Allow();
        }

        var rules = await _ruleRepository.GetEnabledAsync(cancellationToken).ConfigureAwait(false);
        var effectiveLimit = int.MaxValue;
        var effectiveWindow = TimeSpan.FromSeconds(60);
        var effectiveBurst = 0;
        RateLimitRule? matchedRule = null;

        foreach (var rule in rules)
        {
            if (!RuleApplies(rule, context))
            {
                continue;
            }

            var limit = rule.RequestsPerWindow + rule.BurstAllowance;

            if (limit < effectiveLimit)
            {
                effectiveLimit = limit;
                effectiveWindow = TimeSpan.FromSeconds(rule.WindowSeconds);
                effectiveBurst = rule.BurstAllowance;
                matchedRule = rule;
            }
        }

        if (matchedRule is null)
        {
            return Allow();
        }

        var key = BuildKey(matchedRule, context);
        var counter = await _rateLimitStore.IncrementAsync(key, effectiveWindow, cancellationToken).ConfigureAwait(false);

        if (counter.Count <= effectiveLimit)
        {
            return new RateLimitResult
            {
                Allowed = true,
                Remaining = Math.Max(0, effectiveLimit - (int)counter.Count),
                ResetAt = counter.WindowEnd,
                Reason = null,
                EscalatedToBlock = false
            };
        }

        var escalated = false;

        if (counter.Count > effectiveLimit * 2)
        {
            await EscalateAsync(context, effectiveWindow, cancellationToken).ConfigureAwait(false);
            escalated = true;
        }

        return new RateLimitResult
        {
            Allowed = false,
            Remaining = 0,
            ResetAt = counter.WindowEnd,
            Reason = "Rate limit exceeded.",
            EscalatedToBlock = escalated
        };
    }

    public async Task<IReadOnlyList<RateLimitRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return rules.Select(MapRule).ToList().AsReadOnly();
    }

    public async Task<RateLimitRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return rule is null ? null : MapRule(rule);
    }

    public async Task<RateLimitRuleDto> CreateRuleAsync(CreateRateLimitRuleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var rule = new RateLimitRule
        {
            ScopeType = request.ScopeType,
            ScopeValue = request.ScopeValue,
            RequestsPerWindow = request.RequestsPerWindow,
            WindowSeconds = request.WindowSeconds,
            BurstAllowance = request.BurstAllowance
        };

        await _ruleRepository.AddAsync(rule, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapRule(rule);
    }

    public async Task<RateLimitRuleDto> UpdateRuleAsync(Guid id, CreateRateLimitRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rate limit rule not found.");

        ValidateRequest(request);

        rule.RequestsPerWindow = request.RequestsPerWindow;
        rule.WindowSeconds = request.WindowSeconds;
        rule.BurstAllowance = request.BurstAllowance;
        rule.IsEnabled = true;

        await _ruleRepository.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapRule(rule);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rate limit rule not found.");

        await _ruleRepository.DeleteAsync(rule, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool RuleApplies(RateLimitRule rule, RateLimitRequestContext context)
    {
        return rule.ScopeType switch
        {
            RateLimitScopeType.Global => true,
            RateLimitScopeType.Ip => context.IpAddress.Equals(rule.ScopeValue, StringComparison.OrdinalIgnoreCase),
            RateLimitScopeType.User => context.UserId.HasValue && context.UserId.Value.ToString().Equals(rule.ScopeValue, StringComparison.OrdinalIgnoreCase),
            RateLimitScopeType.Device => context.DeviceId.HasValue && context.DeviceId.Value.ToString().Equals(rule.ScopeValue, StringComparison.OrdinalIgnoreCase),
            RateLimitScopeType.Domain => context.Domain.Equals(rule.ScopeValue, StringComparison.OrdinalIgnoreCase),
            RateLimitScopeType.Endpoint => context.Endpoint.Equals(rule.ScopeValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string BuildKey(RateLimitRule rule, RateLimitRequestContext context)
    {
        var scopeIdentifier = rule.ScopeType switch
        {
            RateLimitScopeType.Global => "global",
            RateLimitScopeType.Ip => context.IpAddress,
            RateLimitScopeType.User => context.UserId?.ToString() ?? "anonymous",
            RateLimitScopeType.Device => context.DeviceId?.ToString() ?? "none",
            RateLimitScopeType.Domain => context.Domain,
            RateLimitScopeType.Endpoint => context.Endpoint,
            _ => "unknown"
        };

        return $"ratelimit:{(int)rule.ScopeType}:{scopeIdentifier}";
    }

    private async Task EscalateAsync(RateLimitRequestContext context, TimeSpan duration, CancellationToken cancellationToken)
    {
        var existing = await _blocklistRepository.GetByTypeAndValueAsync(BlocklistEntryType.Ip, context.IpAddress, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            return;
        }

        var entry = new BlocklistEntry
        {
            Type = BlocklistEntryType.Ip,
            Value = context.IpAddress,
            Reason = "Automatic escalation from rate limiting",
            ExpiresAt = DateTimeOffset.UtcNow.Add(duration)
        };

        await _blocklistRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateRequest(CreateRateLimitRuleRequest request)
    {
        if (request.RequestsPerWindow <= 0)
        {
            throw new ArgumentException("Requests per window must be greater than zero.", nameof(request));
        }

        if (request.WindowSeconds <= 0)
        {
            throw new ArgumentException("Window seconds must be greater than zero.", nameof(request));
        }

        if (request.BurstAllowance < 0)
        {
            throw new ArgumentException("Burst allowance cannot be negative.", nameof(request));
        }
    }

    private static RateLimitResult Allow()
    {
        return new RateLimitResult
        {
            Allowed = true,
            Remaining = int.MaxValue,
            ResetAt = DateTimeOffset.UtcNow,
            Reason = null,
            EscalatedToBlock = false
        };
    }

    private static RateLimitRuleDto MapRule(RateLimitRule rule)
    {
        return new RateLimitRuleDto
        {
            Id = rule.Id,
            ScopeType = rule.ScopeType,
            ScopeValue = rule.ScopeValue,
            RequestsPerWindow = rule.RequestsPerWindow,
            WindowSeconds = rule.WindowSeconds,
            BurstAllowance = rule.BurstAllowance,
            IsEnabled = rule.IsEnabled,
            CreatedAt = rule.CreatedAt
        };
    }
}
