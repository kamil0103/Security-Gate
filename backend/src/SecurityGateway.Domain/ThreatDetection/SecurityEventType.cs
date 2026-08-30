namespace SecurityGateway.Domain.ThreatDetection;

public enum SecurityEventType
{
    AuthenticationFailure,
    AccountLocked,
    RateLimitExceeded,
    WafEvent,
    AccessBlocked,
    UnknownDevice,
    NewDeviceFromUntrustedNetwork,
    IpReputationChanged,
    PolicyViolation,
    Custom
}
