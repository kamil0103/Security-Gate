# ADR-011: Device Identity Model

## Status

Accepted

## Context

The Security Gateway must identify and trust devices, not just IP addresses. A user may have multiple devices, and a device may have multiple IP addresses over time. Browser fingerprinting alone is probabilistic and must not be treated as absolute identity.

## Decision

Implement a `Device` entity linked to a `User`. Each device captures:

- A stable device identifier (client-provided `CredentialId`)
- A browser/device fingerprint hash
- User-Agent, operating system, and browser information
- An optional public key for future cryptographic credentials
- A trust status: Pending, Trusted, Untrusted, or Blocked
- An IP history linked to the device

Enrollment rules:

- The first device for a user is automatically trusted.
- Subsequent devices start as Pending and require user approval.
- A known device is recognized by its fingerprint or device ID.
- Fingerprint changes are tolerated; the device record is updated rather than rejected.

## Alternatives

- **IP-only identity:** Insufficient; users may have dynamic IPs or use VPNs.
- **Fingerprint-only identity:** Too rigid; fingerprints change with browser updates.
- **Cookie-only identity:** Vulnerable to cookie theft and does not capture device signals.

## Consequences

- Device trust is independent of IP trust.
- The model supports future WebAuthn/passkey credentials via the `PublicKey` field.
- Pending device approvals are required for new devices, improving security.
- IP history is tied to the device, enabling better behavioral analysis later.
