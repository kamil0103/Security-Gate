# ADR-024 — Advanced Security (V3)

## Status

Accepted

## Context

With core security features in place, the next layer involves external threat intelligence, behavioral anomaly detection, passkey authentication, and integration with community-driven security tools like CrowdSec. We needed to add these capabilities without disrupting existing flows.

## Decision

We implemented several advanced security building blocks.

Key design points:

- **External threat intelligence**: `IThreatIntelligenceProvider` abstraction with a composite `IThreatIntelligenceService`. An `AbuseIpDbThreatIntelligenceProvider` fetches AbuseIPDB reputation data when an API key is configured. Results are merged into IP intelligence threat scores during IP creation.
- **Behavioral analysis**: `IBehavioralAnalysisService` detects request bursts per IP using a sliding in-memory window. The service is registered as a singleton and exposed via `BehavioralAnalysisController`.
- **CrowdSec integration**: `ICrowdSecClient` and a stub implementation define the interface for checking and reporting IPs to a CrowdSec local API. Real HTTP calls are deferred until a CrowdSec service is added to the deployment.
- **Passkeys / WebAuthn**: Added `WebAuthnCredential` domain entity, repository, `IWebAuthnService` stub, and `WebAuthnController`. The service generates challenges and stores credential metadata; cryptographic verification is deferred to a future WebAuthn library integration.
- **Controllers**: Admin controllers for threat intelligence lookup and behavioral analysis; an authorized controller for users to manage their WebAuthn credentials.

## Consequences

- **Pros**:
  - Pluggable threat intelligence providers can be added without changing IP intelligence internals.
  - Behavioral analysis provides a lightweight anomaly signal.
  - WebAuthn and CrowdSec stubs establish clear extension points.

- **Cons**:
  - Behavioral analysis uses in-memory state, so it is not shared across multiple backend instances.
  - WebAuthn implementation is a stub and does not perform real cryptographic attestation/verification.
  - CrowdSec integration is not wired to request handling yet.

## Alternatives Considered

- **Full WebAuthn library integration**: Deferred due to complexity and dependency on frontend attestation flows.
- **Redis-backed behavioral store**: Deferred because the current in-memory window is sufficient for a single-instance deployment.
