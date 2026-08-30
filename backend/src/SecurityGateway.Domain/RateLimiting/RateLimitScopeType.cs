namespace SecurityGateway.Domain.RateLimiting;

public enum RateLimitScopeType
{
    Global,
    Ip,
    User,
    Device,
    Domain,
    Endpoint
}
