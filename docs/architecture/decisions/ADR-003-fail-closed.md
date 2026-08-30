# ADR-003: Fail-Closed Architecture

## Status

Accepted

## Context

The Security Gateway is the enforcement point in front of Nginx Proxy Manager. If the gateway fails, external traffic must not bypass security controls.

## Decision

Implement a fail-closed architecture. If the Security Gateway is unavailable, external applications become inaccessible.

## Alternatives

- **Fail-open:** If the gateway fails, traffic bypasses it to NPM. Rejected because it defeats the security purpose.
- **Standalone gateway appliance:** Could be implemented later, but the logical requirement remains fail-closed.

## Consequences

- NPM must not be directly reachable from the public Internet.
- The gateway must be designed for high availability and observability.
- A secure local Unraid administration path must be maintained for recovery.
- Fail-closed behavior must be tested and documented.
