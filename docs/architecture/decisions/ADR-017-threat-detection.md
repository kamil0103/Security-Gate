# ADR-017: Threat Detection and Scoring

## Status
Accepted

## Context
Security Gateway collects many security signals: authentication failures, rate-limit violations, WAF events, blocklist hits, and device/IP anomalies. To act on these signals, the system needs a unified security event feed and a threat scoring engine that can identify suspicious or malicious actors.

## Decision
Introduce a threat detection subsystem:

- `SecurityEvent` entity captures a unified log of security-relevant occurrences.
- `ThreatScoreRule` entity defines behavioral rules: event type + count threshold within a time window → score impact.
- `IThreatDetectionService` provides:
  - `RecordEventAsync` to store events and evaluate threat score
  - `EvaluateThreatScoreAsync` to re-evaluate an IP against all enabled rules
  - CRUD for rules and search/recent queries for events
- The service is integrated into existing flows:
  - `AuthenticationService` records authentication failures and access-blocked logins.
  - `RateLimitService` records rate-limit exceeded events.
  - `WafEventService` records high-severity WAF events.
  - `AccessControlService` records blocklist matches.
- When a rule threshold is met, the source IP's threat score and level are updated in the IP intelligence store.

## Consequences

### Positive
- **Unified telemetry**: all security signals flow into one event store.
- **Behavioral scoring**: simple count/window rules can detect brute force, scanning, and repeated abuse.
- **Automatic reputation updates**: IP intelligence stays current without manual intervention.
- **Foundation for automated response**: Phase 11 (automatic blocking) can consume threat scores directly.

### Negative
- **Rules are simple counters**: no machine learning or anomaly detection in V1.
- **Score increases only**: there is no decay mechanism yet; old offenders remain high-scored until manually cleared.
- **Best-effort logging**: event recording is wrapped in try/catch so failures do not break primary flows, but events could be lost.

## Related
- `SecurityGateway.Domain/ThreatDetection/`
- `SecurityGateway.Application/ThreatDetection/`
- `SecurityGateway.Infrastructure/ThreatDetection/`
- `SecurityGateway.Api/Controllers/SecurityEventsController.cs`
- `SecurityGateway.Api/Controllers/ThreatScoreRulesController.cs`
