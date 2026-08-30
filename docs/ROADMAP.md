# Security Gateway — Roadmap

## Phase 0 — Project Setup

- [x] Repository initialized
- [x] Documentation: README, spec, roadmap, start here, contributing, security
- [x] Architecture Decision Records
- [x] GitHub Actions CI scaffolding
- [x] Branching and commit conventions documented

**Milestone:** Repository is ready for development.

## Phase 1 — Development Infrastructure

- [x] React 19 + TypeScript + Vite frontend
- [x] ASP.NET Core 9 backend with Clean Architecture
- [x] PostgreSQL 16 in Docker Compose
- [x] Redis 7 in Docker Compose
- [x] Backend health endpoint verifying Postgres and Redis connectivity
- [x] Frontend health page communicating with backend
- [x] Docker Compose development environment
- [x] Backend integration tests
- [x] Frontend smoke tests
- [x] CI pipeline passing

**Milestone:** Every developer can clone the repository and run the entire development environment.

## Phase 2 — Gateway Foundation

- [x] Gateway networking and request handling
- [x] Trusted proxy chain configuration
- [x] Client IP extraction and validation
- [x] NPM communication
- [x] Logging
- [x] Fail-closed networking design

**Milestone:** The gateway can receive requests, resolve the real client IP from trusted proxies, log requests, and forward them to Nginx Proxy Manager.

## Phase 3 — Authentication

- [x] Local accounts
- [x] Login/logout
- [x] Sessions (expiration, revocation)
- [x] Password change and reset
- [x] Email verification
- [x] SMTP configuration

**Milestone:** Users can register, log in, manage sessions, change/reset passwords, and verify email addresses.

## Phase 4 — Device Identity

- [x] Device enrollment
- [x] Device identity signals
- [x] Device credentials
- [x] Fingerprinting tolerance
- [x] Device/user relationships
- [x] Device management API

**Milestone:** Devices are enrolled during authentication, recognized by fingerprint or device ID, and can be trusted, untrusted, blocked, or removed by the user.

## Phase 5 — IP Intelligence ✅

- IP tracking (request counts, first/last seen, user/device associations)
- GeoIP abstraction (`IGeoIpProvider`) with a null default implementation
- ASN/ISP lookup fields on the `IpAddress` entity
- VPN/proxy/Tor detection abstraction (`IVpnProxyDetector`) with a null default implementation
- Reputation provider abstraction (`IReputationProvider`) with a null default implementation
- `IpAddress` entity with IP ↔ user/device associations
- `IpController` (`GET /api/ip/me`, `GET /api/ip/recent`, `GET /api/ip/{id}`)
- EF Core migration `AddIpIntelligence`

**Milestone:** Every proxied request is tracked, enriched with GeoIP, reputation, and VPN metadata via swappable providers, and exposed through a read-only API.

## Phase 6 — Access Control ✅

- Unknown device challenge handled during login/register
- Approval/denial workflow for pending devices (`AccessDecision`)
- Trusted networks (`TrustedNetwork`) with CIDR matching
- Blocking workflow (`BlocklistEntry`) for IPs, networks, devices, and users
- `AccessControlService` integrating trust evaluation with device status
- `AccessControlController` for administrators to manage networks, blocklist, and device decisions
- EF Core migration `AddAccessControl`

**Milestone:** Administrators can define trusted networks and blocklist entries, new devices on trusted networks are auto-approved, and blocked IPs/devices/users are denied access at login.

## Phase 7 — Application Policies ✅

- `Application` entity with domain, name, upstream URL, and enabled status
- `ApplicationPolicy` entity with per-application settings:
  - Authentication requirement
  - Anonymous access from trusted networks
  - Allowed/blocked countries and IP addresses
- `IApplicationPolicyService` for CRUD and policy evaluation
- Gateway middleware resolves applications by `Host` header, evaluates policy, and routes to per-application upstream URLs
- `ApplicationsController` and `ApplicationPoliciesController` for admin configuration
- EF Core migration `AddApplications`

**Milestone:** Each proxied domain can be configured independently, and the gateway enforces authentication and IP-based access rules before forwarding traffic.

## Phase 8 — Rate Limiting

- Redis-backed rate limiting
- IP/user/device/domain/endpoint limits
- Temporary throttling and bans
- Automatic escalation

## Phase 9 — WAF

- ModSecurity + OWASP CRS integration
- WAF event consumption
- Attack classification

## Phase 10 — Threat Detection

- Threat scoring engine
- Behavioral rules
- Security events
- Automatic response triggers

## Phase 11 — Automatic Blocking

- Temporary and permanent blocks
- Escalation
- Manual controls
- Block expiration and audit trail

## Phase 12 — Dashboard

- Statistics, charts, and tables
- Real-time events
- Security timeline
- Application/IP/device/attack statistics

## Phase 13 — Global Map

- GeoIP visualization
- Attack visualization
- IP explorer
- Filters

## Phase 14 — Notifications

- SMTP/email
- Telegram
- Discord
- ntfy
- Web push

## Phase 15 — Audit

- Audit logging
- Search and filtering
- Security history

## Phase 16 — Production Hardening

- Security testing
- Penetration testing
- Docker hardening
- Backup/recovery
- Fail-closed validation

## Phase 17 — V3 Advanced Security

- CrowdSec integration
- External threat intelligence
- Behavioral analysis
- Passkeys / WebAuthn
- Advanced device trust
- Advanced WAF functionality

## Phase 18 — Cloudflare

- Cloudflare integration
- Real client IP handling
- Per-application routing
- Streaming bypass
- Cloudflare-aware policies
