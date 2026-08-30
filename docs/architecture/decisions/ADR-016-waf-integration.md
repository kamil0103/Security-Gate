# ADR-016: WAF Integration and Attack Classification

## Status
Accepted

## Context
Security Gateway needs a Web Application Firewall layer to detect and block common web attacks. The chosen WAF is ModSecurity with the OWASP Core Rule Set (CRS), deployed as a separate container. Security Gateway must consume WAF events, classify attacks, and correlate them with IP intelligence to improve threat detection and response.

## Decision
Run ModSecurity + OWASP CRS as a separate container (`modsecurity-crs`) that can sit in front of the gateway. Security Gateway exposes an ingestion endpoint (`POST /api/waf-events`) that receives WAF events from log shippers or the CRS container. Internally, the system:

- Stores `WafEvent` records in PostgreSQL.
- Uses an `IAttackClassifier` to map ModSecurity rule IDs and messages to `AttackType` and `AttackSeverity`.
- Correlates events with the `IpAddress` entity, incrementing `AttackCount` and updating threat score/level for high-severity events.
- Provides admin endpoints to search and review WAF events.

The classifier in V1 recognizes OWASP CRS categories including SQL injection, XSS, LFI/RFI, RCE, command injection, path traversal, brute force, bots, and scanning.

## Consequences

### Positive
- **Decoupled WAF**: ModSecurity can be upgraded or replaced independently of the gateway.
- **Rich audit trail**: all WAF events are persisted and searchable.
- **Threat enrichment**: IP intelligence is strengthened with attack history.
- **Extensible classifier**: new attack types and rule mappings can be added without changing the domain model.

### Negative
- **Manual integration**: the CRS container must be configured to forward events to the ingestion endpoint.
- **No real-time blocking feedback loop in V1**: ingested events update IP reputation but do not immediately block subsequent requests.
- **Classifier is rule-based**: advanced behavioral detection is left to future threat-detection work.

## Related
- `SecurityGateway.Domain/Waf/`
- `SecurityGateway.Application/Waf/`
- `SecurityGateway.Infrastructure/Waf/`
- `SecurityGateway.Api/Controllers/WafEventsController.cs`
- `docker-compose.yml` (modsecurity-crs service)
