namespace SecurityGateway.Domain.AccessControl;

public enum AccessRequestStatus
{
    Pending,
    Approved,
    Denied,
    Expired,
    Cancelled
}

public enum AccessRequestDecision
{
    Approve,
    Deny,
    BlockIp,
    BlockDevice
}

public enum ApprovalScope
{
    Once,
    Session,
    Device,
    IpAndDevice,
    Ip,
    Permanent
}
