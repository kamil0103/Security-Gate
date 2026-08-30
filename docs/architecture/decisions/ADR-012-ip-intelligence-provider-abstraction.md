# ADR-012: IP Intelligence Provider Abstraction

## Status
Accepted

## Context
Security Gateway needs to collect and act on IP-level intelligence to support future threat detection, access control, and audit logging. Capabilities include:

- Geo-location (country, region, city, coordinates)
- ASN/ISP identification
- VPN, proxy, Tor, and datacenter detection
- IP reputation scoring and threat-level classification

These capabilities typically depend on third-party services or local databases (e.g., MaxMind GeoIP2, IPinfo, IPQualityScore, AbuseIPDB). Tightly coupling the system to a single provider would make swapping, testing, or self-hosting intelligence data difficult.

## Decision
Introduce a set of provider abstractions in the Application layer:

- `IGeoIpProvider` — resolves geographic and network ownership data for an IP address.
- `IVpnProxyDetector` — determines whether an IP belongs to a VPN, proxy, Tor exit node, or datacenter.
- `IReputationProvider` — returns a threat score and level for an IP address.

The Infrastructure layer ships with null/default implementations (`NullGeoIpProvider`, `NullVpnProxyDetector`, `NullReputationProvider`) so the system works out of the box without external API keys. Real providers can be implemented later as drop-in replacements by registering them in `Program.cs`.

A domain entity, `IpAddress`, stores the enriched data along with request/attack/block counters, first/last seen timestamps, and associations with users and devices. An `IIpIntelligenceService` orchestrates lookup, enrichment, and persistence.

## Consequences

### Positive
- **Provider independence**: External services can be swapped without touching domain or application logic.
- **Testability**: Null providers and in-memory repositories make unit and integration tests fast and deterministic.
- **Gradual rollout**: Production can start with no external dependencies and enable real providers once configured.
- **Extensibility**: New providers (local MaxMind database, self-hosted IP feeds) can be added in a single Infrastructure class.

### Negative
- **No real intelligence by default**: Until a real provider is wired in, all GeoIP/reputation/VPN fields are empty or neutral.
- **Additional orchestration code**: The service must coordinate multiple providers and handle partial failures gracefully.

## Related
- `SecurityGateway.Application/IpIntelligence/`
- `SecurityGateway.Infrastructure/IpIntelligence/Providers/`
- `SecurityGateway.Domain/IpIntelligence/IpAddress.cs`
