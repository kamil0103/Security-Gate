# ADR-007: Fail-Closed Network Architecture

## Status

Accepted

## Context

The Security Gateway is the enforcement point. If it becomes unavailable, external traffic must not bypass security controls by reaching Nginx Proxy Manager directly.

## Decision

Adopt a fail-closed architecture:

- Nginx Proxy Manager must not be directly reachable from the public Internet.
- Public DNS and port forwarding must point only to the Security Gateway.
- If the gateway fails, external access is blocked because no path exists to NPM.
- A secure local administration path (e.g., Unraid local console, local network SSH/VPN) is maintained for recovery.

In the current development environment, this is simulated by:

- The gateway proxying to an NPM placeholder container.
- NPM placeholder not being exposed as the public entry point.

In production on Unraid, this is enforced by:

- Running NPM in a Docker network that is not port-forwarded from the router.
- Running the Security Gateway in a container that is port-forwarded and has network access to NPM.
- Router/firewall rules that block direct inbound access to NPM.

## Alternatives

- **Fail-open:** If the gateway fails, traffic bypasses it. Rejected because it defeats the security purpose.
- **Gateway as the only container with published ports:** Equivalent approach; the gateway must be the sole public entry point.

## Consequences

- Gateway availability directly impacts application availability.
- Monitoring and alerting on gateway health are critical.
- Local recovery procedures must be documented and tested.
