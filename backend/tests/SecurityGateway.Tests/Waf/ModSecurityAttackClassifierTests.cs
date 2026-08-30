using SecurityGateway.Domain.Waf;
using SecurityGateway.Infrastructure.Waf.Services;
using Xunit;

namespace SecurityGateway.Tests.Waf;

public class ModSecurityAttackClassifierTests
{
    private readonly ModSecurityAttackClassifier _classifier = new();

    [Theory]
    [InlineData("942100", "SQL Injection Attack", AttackType.SqlInjection)]
    [InlineData("941100", "XSS Attack", AttackType.CrossSiteScripting)]
    [InlineData("930100", "Path Traversal", AttackType.PathTraversal)]
    [InlineData("931100", "RFI", AttackType.RemoteFileInclusion)]
    [InlineData("932100", "RCE", AttackType.RemoteCodeExecution)]
    [InlineData("933100", "PHP Injection", AttackType.RemoteCodeExecution)]
    [InlineData("934100", "Command Injection", AttackType.CommandInjection)]
    [InlineData("999999", "Unknown anomaly", AttackType.Other)]
    public void Classify_ReturnsExpectedAttackType(string ruleId, string message, AttackType expected)
    {
        var result = _classifier.Classify(ruleId, message);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("942100", "SQL injection", AttackSeverity.Critical)]
    [InlineData("941100", "XSS", AttackSeverity.High)]
    [InlineData("920100", "Protocol violation", AttackSeverity.Medium)]
    [InlineData("980100", "low severity", AttackSeverity.Low)]
    [InlineData("999999", "", AttackSeverity.Info)]
    public void ClassifySeverity_ReturnsExpectedSeverity(string ruleId, string message, AttackSeverity expected)
    {
        var result = _classifier.ClassifySeverity(ruleId, message);
        Assert.Equal(expected, result);
    }
}
