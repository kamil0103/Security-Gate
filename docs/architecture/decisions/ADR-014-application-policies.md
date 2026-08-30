# ADR-014: Application Policies and Per-Domain Routing

## Status
Accepted

## Context
Security Gateway sits in front of multiple applications (e.g., Immich, Nextcloud, Jellyfin). Each application has its own domain and may require different security postures:

- Some applications require authentication; others may allow anonymous access from trusted networks.
- Some applications should only be reachable from specific countries or IP ranges.
- Each application may forward to a different upstream service.

The gateway must therefore resolve the target application from the incoming request and apply the appropriate policy before forwarding.

## Decision
Introduce an `Application` entity that maps a domain to an upstream URL, and an `ApplicationPolicy` entity that stores per-application security settings. The Application layer exposes `IApplicationPolicyService` for:

- CRUD operations on applications and policies
- Policy evaluation given a client IP and authentication state

The gateway middleware resolves the application by the `Host` header, evaluates its policy, and forwards the request to the application-specific upstream URL. If no application matches, the gateway falls back to the configured default upstream (Nginx Proxy Manager).

V1 policy rules include:

- Application enabled/disabled
- Authentication required
- Allow anonymous access from trusted networks
- Allowed/blocked IP address lists
- Placeholder fields for country-based rules (GeoIP integration in a future phase)

## Consequences

### Positive
- **Per-application control**: different domains can have different security requirements.
- **Centralized policy evaluation**: the same service is used by the gateway and administrative APIs.
- **Flexible upstream routing**: each application can forward to a distinct backend service.
- **Foundation for future features**: rate limiting, WAF, and geo restrictions can build on the application/policy model.

### Negative
- **Host header dependency**: routing relies on clients sending the correct `Host` header.
- **IPv4-only IP lists in V1**: CIDR ranges and IPv6 addresses are not supported in the initial allowlist/blocklist fields.
- **No real-time policy caching**: policies are loaded from the database on every request; caching can be added later.

## Related
- `SecurityGateway.Domain/Applications/`
- `SecurityGateway.Application/Applications/`
- `SecurityGateway.Infrastructure/Applications/`
- `SecurityGateway.Api/Controllers/ApplicationsController.cs`
- `SecurityGateway.Api/Controllers/ApplicationPoliciesController.cs`
- `SecurityGateway.Api/Middleware/GatewayMiddleware.cs`
