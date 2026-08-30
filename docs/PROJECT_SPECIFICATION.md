# Security Gateway — Project Specification

## 1. Overview

Security Gateway is a self-hosted security gateway for Unraid servers. It sits in front of Nginx Proxy Manager and decides whether each incoming request is allowed to proceed.

## 2. Goals

- Protect self-hosted applications behind a single enforcement point.
- Provide strong authentication, authorization, device identity, and IP intelligence.
- Detect and automatically respond to attacks and suspicious behavior.
- Offer a professional security operations dashboard with real-time events and a global map.
- Remain modular and extensible for future V3 features (CrowdSec, Cloudflare, passkeys, behavioral AI).

## 3. Non-Goals

- Replacing Nginx Proxy Manager. NPM remains responsible for TLS termination and reverse proxying.
- Replacing a professional WAF. The gateway integrates ModSecurity + OWASP CRS.
- Supporting OAuth or social login in V1.
- Distributed deployment or Kubernetes support in V1.

## 4. Core Architecture

```
Internet
    ↓
Security Gateway (enforcement point)
    ↓
Nginx Proxy Manager
    ↓
Docker Applications (Immich, Plex, etc.)
```

## 5. Fail-Closed Requirement

If the Security Gateway becomes unavailable, external access must be blocked. Direct access to NPM from the Internet is not permitted.

A secure local administration path must exist for recovery.

## 6. Authentication (V1)

- Local accounts only.
- Username, email, password, role, account status.
- Login, logout, session management, expiration, revocation.
- Password change, password reset, email verification.
- Configurable SMTP.
- Architecture must support future TOTP, passkeys, and WebAuthn.

## 7. Device Identity

Combine multiple signals:

- Device identifier
- Cryptographic device credential
- Browser/device fingerprint
- User-Agent information
- Operating system
- IP address history
- User account
- Login session

Browser fingerprinting is probabilistic and must not be treated as absolute identity.

## 8. IP Intelligence

Track external IP addresses with:

- First/last seen
- Associated users/devices
- Request/attack/block counts
- GeoIP (country, region, city, latitude, longitude)
- ISP and ASN
- VPN/proxy/Tor status
- Threat/reputation information

GeoIP and reputation providers must be abstracted so providers can be swapped.

## 9. Trust Model

Connection state:

```
UNKNOWN → CHALLENGED → AUTHENTICATED → TRUSTED
```

Threat state:

```
NORMAL → SUSPICIOUS → ATTACK → BLOCKED
```

Trust considers user, IP, device, session, application, previous behavior, and security events.

## 10. New Device / New IP Flow

1. Unknown device/IP attempts access.
2. Authentication challenge.
3. Username/password verification.
4. Additional verification when required.
5. Device enrollment.
6. Approval/trust decision.
7. Access granted or denied.

Trusted administrators can approve/deny pending requests from the dashboard.

## 11. Application-Specific Security Policies

Each proxied application/domain is independently configurable for:

- Authentication requirements
- IP rules
- Device rules
- Rate limits
- WAF configuration
- Geo restrictions
- Trusted networks

## 12. Rate Limiting

Redis-backed rate limiting based on:

- IP
- User
- Device
- Domain
- Endpoint
- Authentication endpoint

Support burst limits, temporary throttling, temporary bans, automatic escalation, and configurable thresholds.

## 13. WAF

Integrate ModSecurity with OWASP Core Rule Set. The gateway consumes WAF events and converts them into security events.

## 14. Threat Detection Engine

A dedicated subsystem that consumes:

- IP reputation
- GeoIP
- VPN/proxy/Tor status
- Device identity
- Authentication events
- Request frequency
- Rate-limit violations
- WAF events
- Previous security events
- Application target
- Request behavior

Output: threat score and severity (LOW, MEDIUM, HIGH, CRITICAL; NORMAL, SUSPICIOUS, ATTACK).

## 15. Automatic Response

Security events trigger policy evaluation resulting in:

- Temporary block
- Permanent block
- Automatic escalation
- Manual unblock
- Block expiration
- Block reason
- Audit trail

## 16. Global Map

Dashboard map visualizing incoming traffic, suspicious activity, attacks, blocked IPs, and trusted activity using GeoIP data.

## 17. Dashboard

Professional security operations dashboard with:

- Real-time events via SignalR/WebSockets
- Statistics and charts
- Security timeline
- Application/IP/device/attack statistics
- Pending approvals
- Global map

## 18. Notifications

Abstract notification provider supporting:

- SMTP/email
- Telegram
- Discord
- ntfy
- Web push

Configurable notification rules by severity.

## 19. Audit Logging

Log security-critical actions with timestamp, user, IP, device, action, target, result, and metadata. Never store sensitive secrets in logs.

## 20. Security Requirements

- Never store plaintext passwords.
- Never commit secrets.
- Never trust arbitrary forwarded headers.
- Validate trusted proxy chains for client IP headers.
- Do not expose PostgreSQL, Redis, or NPM publicly.
- Use secure cookies, security headers, input validation, authorization, CSRF protection, rate limiting, secure password hashing, and Docker network isolation.
