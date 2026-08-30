namespace SecurityGateway.Domain.Identity;

public enum UserStatus
{
    PendingVerification = 0,
    Active = 1,
    Suspended = 2,
    Disabled = 3
}
