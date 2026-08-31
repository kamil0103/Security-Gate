using SecurityGateway.Domain.Waf;

namespace SecurityGateway.Application.Waf;

public interface IAttackClassifier
{
    AttackType Classify(string ruleId, string? ruleMessage);
    AttackSeverity ClassifySeverity(string ruleId, string? ruleMessage);
}
