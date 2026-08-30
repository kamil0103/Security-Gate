namespace SecurityGateway.Domain.Waf;

public enum AttackType
{
    Unknown,
    SqlInjection,
    CrossSiteScripting,
    LocalFileInclusion,
    RemoteFileInclusion,
    RemoteCodeExecution,
    CommandInjection,
    PathTraversal,
    BruteForce,
    Bot,
    Scanning,
    Other
}
