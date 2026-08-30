# ADR-015: Rate Limiting

## Status
Accepted

## Context
Security Gateway must protect proxied applications from abuse, brute-force attacks, and accidental overload. Rate limiting should be configurable across multiple dimensions:

- Global
- Per IP address
- Per authenticated user
- Per device
- Per domain/application
- Per endpoint/path

Rate limit state must be shared across multiple gateway instances, so Redis is the natural backing store.

## Decision
Introduce a rate limiting subsystem with the following components:

- `RateLimitRule` entity stored in PostgreSQL for administrative configuration.
- `IRateLimitStore` abstraction with a `RedisRateLimitStore` implementation using fixed-window counters.
- `IRateLimitService` that:
  - Loads enabled rules
  - Matches the request context against applicable rules
  - Increments counters and decides whether to allow or deny
  - Escalates repeat offenders to temporary `BlocklistEntry` records when requests exceed 2x the configured limit
- `RateLimitController` for administrators to manage rules.
- Integration into `GatewayMiddleware` after application policy evaluation, returning HTTP 429 when a limit is exceeded.

The store uses fixed-window counters with Redis keys scoped as `ratelimit:{scopeType}:{scopeIdentifier}:{windowStart}`. Each key is incremented and given an expiry matching the window duration.

## Consequences

### Positive
- **Shared state**: Redis keeps counters consistent across gateway instances.
- **Flexible scope rules**: different limits can be applied to IPs, users, devices, domains, endpoints, or globally.
- **Self-protecting**: automatic escalation reduces the load of repeated abuse.
- **Graceful fallback**: if Redis is unavailable, the service allows traffic rather than blocking it.

### Negative
- **Fixed windows**: traffic spikes at window boundaries are possible; sliding-window support can be added later.
- **No real-time rule caching**: rules are loaded from PostgreSQL on every request; caching can be added when needed.
- **Escalation is IP-only in V1**: future versions could escalate by user or device.

## Related
- `SecurityGateway.Domain/RateLimiting/`
- `SecurityGateway.Application/RateLimiting/`
- `SecurityGateway.Infrastructure/RateLimiting/`
- `SecurityGateway.Api/Controllers/RateLimitController.cs`
- `SecurityGateway.Api/Middleware/GatewayMiddleware.cs`
