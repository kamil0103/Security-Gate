# ADR-004: Use ModSecurity with OWASP Core Rule Set

## Status

Accepted

## Context

The gateway must detect and block common web attacks such as SQL injection, XSS, path traversal, command injection, and scanner activity.

## Decision

Integrate ModSecurity with the OWASP Core Rule Set (CRS) rather than building a custom WAF engine.

## Alternatives

- **Custom WAF logic:** Would require constant maintenance and would likely be less effective than established rule sets.
- **Commercial WAF:** Not suitable for a self-hosted, open-source project.

## Consequences

- ModSecurity + CRS provides mature attack detection.
- The gateway consumes WAF events and converts them into internal security events.
- The WAF is one input to the threat detection engine, not the only control.
