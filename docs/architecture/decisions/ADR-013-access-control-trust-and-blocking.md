# ADR-013: Access Control, Trust, and Blocking

## Status
Accepted

## Context
Security Gateway needs to decide whether a given request or authentication attempt should be allowed, challenged, or blocked. Decisions depend on multiple signals:

- Whether the device is known and trusted
- Whether the source IP belongs to a trusted network
- Whether the IP, device, user, or network is explicitly blocklisted
- Administrative approval/denial decisions for pending devices

A centralized access control service prevents these rules from being scattered across authentication, gateway, and device identity code.

## Decision
Introduce an `IAccessControlService` in the Application layer with the following responsibilities:

1. **Trusted network evaluation** — determine whether an IP falls within an enabled `TrustedNetwork` CIDR.
2. **Blocklist evaluation** — check active `BlocklistEntry` records of type `Ip`, `Network`, `Device`, or `User`.
3. **Device trust evaluation** — combine blocklist, device trust status, and trusted network information to produce a `DeviceTrustResult`.
4. **Administrative workflows** — provide `ApproveDeviceAsync` and `DenyDeviceAsync` operations that update the device status and persist an `AccessDecision` audit record.
5. **CRUD operations** for trusted networks and blocklist entries.

New devices enroll with `DeviceTrustStatus.Pending`. During login/register, `AuthenticationService` calls `EvaluateDeviceTrustAsync`. If the device is pending and originates from a trusted network, it is automatically promoted to `Trusted`. Otherwise it remains `Pending` until an administrator approves or denies it. Blocked users, devices, or IPs are rejected during authentication.

## Consequences

### Positive
- **Centralized policy**: all trust/block decisions route through one service.
- **Audit trail**: every approve/deny action creates an `AccessDecision` record.
- **Flexibility**: trusted networks and blocklist entries are runtime-configurable by administrators.
- **Clear integration points**: authentication and future gateway middleware can call the same service.

### Negative
- **CIDR matching is IPv4-only in V1**: IPv6 support and more robust network parsing can be added later.
- **Trusted-network auto-approval is coarse**: it trusts any device from the network, which may be too permissive for some deployments.

## Related
- `SecurityGateway.Application/AccessControl/`
- `SecurityGateway.Infrastructure/AccessControl/`
- `SecurityGateway.Api/Controllers/AccessControlController.cs`
- `SecurityGateway.Domain/AccessControl/`
