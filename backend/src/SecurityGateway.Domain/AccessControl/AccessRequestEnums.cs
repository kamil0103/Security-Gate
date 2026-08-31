using System.Text.Json.Serialization;

namespace SecurityGateway.Domain.AccessControl;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessRequestStatus
{
    Pending,
    Approved,
    Denied,
    Expired,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessRequestDecision
{
    Approve,
    Deny,
    BlockIp,
    BlockDevice
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApprovalScope
{
    Once,
    Session,
    Device,
    IpAndDevice,
    Ip,
    Permanent
}
