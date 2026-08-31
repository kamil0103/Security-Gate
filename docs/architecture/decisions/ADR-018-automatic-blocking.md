# ADR-018 — Automatic Blocking

## Status

Accepted

## Context

The Security Gateway collects security events, evaluates threat scores, and maintains a blocklist for manual access control. Once an IP accumulates enough suspicious activity, administrators should not have to intervene manually every time. We needed a service that can automatically convert high threat scores into temporary or permanent IP blocks, while still allowing administrators to override decisions and inspect state.

## Decision

We introduced `IAutomaticBlockingService` and its implementation `AutomaticBlockingService`.

Key design points:

- **Threshold-driven auto-blocking**: The service reads `AutomaticBlockingOptions` with score thresholds for medium, high, and critical threat levels and corresponding block durations. When `CheckAndBlockAsync` is called with a threat score, it creates a blocklist entry if the score meets or exceeds the configured threshold.
- **Gateway integration**: The gateway middleware invokes `CheckAndBlockAsync` for every proxied request. If the IP is blocked, the middleware returns HTTP 403 with a reason message.
- **Threat detection integration**: `ThreatDetectionService` calls `CheckAndBlockAsync` whenever a threat score evaluation escalates the IP to a new threshold.
- **Manual controls**: `BlockingController` exposes `POST /api/blocking/block`, `POST /api/blocking/unblock`, and `GET /api/blocking/is-blocked` for administrators.
- **Reuses existing blocklist infrastructure**: Automatic blocks are stored as `BlocklistEntry` records with type `Ip` and an optional expiration time. This gives us an existing audit trail and unifies block management with manual access control blocks.
- **Single-responsibility abstraction**: The service does not compute threat scores; it only decides whether a score warrants a block and manages block lifecycle.

## Consequences

- **Pros**:
  - Centralized, testable automatic blocking logic.
  - Consistent use of the existing blocklist domain model.
  - Configurable thresholds and durations without code changes.
  - Manual overrides use the same records and controllers.

- **Cons**:
  - Automatic and manual blocks share one table; queries must distinguish by reason or source if required later.
  - Block duration configuration is global; per-IP or per-rule durations would require future extensions.

## Alternatives Considered

- **Separate automatic-block table**: Rejected because it would duplicate blocklist logic and complicate gateway enforcement.
- **Blocking inside threat detection service**: Rejected to keep threat scoring and enforcement responsibilities separate.
