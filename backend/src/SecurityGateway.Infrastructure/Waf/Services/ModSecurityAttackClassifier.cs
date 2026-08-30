using SecurityGateway.Application.Waf;
using SecurityGateway.Domain.Waf;

namespace SecurityGateway.Infrastructure.Waf.Services;

public sealed class ModSecurityAttackClassifier : IAttackClassifier
{
    public AttackType Classify(string ruleId, string? ruleMessage)
    {
        var message = ruleMessage ?? string.Empty;

        if (Contains(message, "sql", "sqli"))
        {
            return AttackType.SqlInjection;
        }

        if (Contains(message, "xss", "cross-site scripting", "cross site scripting"))
        {
            return AttackType.CrossSiteScripting;
        }

        if (Contains(message, "lfi", "local file inclusion"))
        {
            return AttackType.LocalFileInclusion;
        }

        if (Contains(message, "rfi", "remote file inclusion"))
        {
            return AttackType.RemoteFileInclusion;
        }

        if (Contains(message, "rce", "remote code execution", "code injection", "php injection"))
        {
            return AttackType.RemoteCodeExecution;
        }

        if (Contains(message, "command injection", "cmd injection", "system command"))
        {
            return AttackType.CommandInjection;
        }

        if (Contains(message, "path traversal", "directory traversal", "../"))
        {
            return AttackType.PathTraversal;
        }

        if (Contains(message, "brute force", "bruteforce", "failed login"))
        {
            return AttackType.BruteForce;
        }

        if (Contains(message, "bot", "crawler", "scanner"))
        {
            return AttackType.Bot;
        }

        if (Contains(message, "scan", "probe", "recon"))
        {
            return AttackType.Scanning;
        }

        return AttackType.Other;
    }

    public AttackSeverity ClassifySeverity(string ruleId, string? ruleMessage)
    {
        var message = (ruleMessage ?? string.Empty).ToUpperInvariant();

        if (message.Contains("CRITICAL") || ruleId.StartsWith("942", StringComparison.Ordinal))
        {
            return AttackSeverity.Critical;
        }

        if (message.Contains("HIGH") || ruleId.StartsWith("941", StringComparison.Ordinal) || ruleId.StartsWith("943", StringComparison.Ordinal))
        {
            return AttackSeverity.High;
        }

        if (message.Contains("MEDIUM") || ruleId.StartsWith("920", StringComparison.Ordinal))
        {
            return AttackSeverity.Medium;
        }

        if (message.Contains("LOW"))
        {
            return AttackSeverity.Low;
        }

        return AttackSeverity.Info;
    }

    private static bool Contains(string input, params string[] values)
    {
        var upper = input.ToUpperInvariant();
        return values.Any(v => upper.Contains(v.ToUpperInvariant()));
    }
}
