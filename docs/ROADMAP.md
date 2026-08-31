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

## Phase 8 — Rate Limiting ✅

- Redis-backed rate limiting via `IRateLimitStore` and `RedisRateLimitStore`
- Rate limit rules by scope: Global, IP, User, Device, Domain, Endpoint
- Fixed-window counters with burst allowance
- Automatic escalation to temporary IP blocklist entries when limits are exceeded by 2x
- `RateLimitService` integrated into the gateway middleware (returns 429 when exceeded)
- `RateLimitController` for admin rule management
- EF Core migration `AddRateLimiting`

**Milestone:** The gateway enforces configurable request rate limits per IP, user, device, domain, and endpoint, with automatic temporary bans for abusers.

## Phase 9 — WAF ✅

- `WafEvent` domain model for ModSecurity/CRS event ingestion
- `IAttackClassifier` abstraction with `ModSecurityAttackClassifier`
- Attack types: SQLi, XSS, LFI, RFI, RCE, command injection, path traversal, brute force, bot, scanning
- `WafEventService` that ingests events, classifies attacks, and correlates with IP intelligence
- `WafEventsController` with anonymous ingestion endpoint and admin search/recent endpoints
- IP intelligence correlation: increments `AttackCount` and updates threat score/level
- Reference `modsecurity-crs` service in `docker-compose.yml`
- EF Core migration `AddWafEvents`

**Milestone:** Security Gateway can consume WAF events from ModSecurity + OWASP CRS, classify attacks, and enrich IP intelligence with attack history.

## Phase 10 — Threat Detection ✅

- `SecurityEvent` and `ThreatScoreRule` domain entities
- `IThreatDetectionService` for recording events and evaluating threat scores
- Behavioral rules: count events of a given type within a time window and apply score impact
- Automatic IP threat score escalation when thresholds are met
- Security event generation integrated into:
  - Authentication failures and access-blocked logins
  - Rate limit exceeded
  - High-severity WAF events
  - Blocklist matches
- `SecurityEventsController` and `ThreatScoreRulesController` for admin review and rule management
- EF Core migration `AddThreatDetection`

**Milestone:** The gateway maintains a unified security event feed, applies behavioral threat scoring rules, and updates IP reputation automatically.

## Phase 11 — Automatic Blocking ✅

- `IAutomaticBlockingService` and `AutomaticBlockingOptions` for threshold-based auto-blocking
- Automatic block decisions driven by threat score levels (medium, high, critical)
- Temporary blocks with configurable durations and permanent manual blocks
- Integration into the gateway middleware (returns 403 for blocked IPs)
- Automatic blocking trigger from `ThreatDetectionService` when score thresholds are met
- `BlockingController` for administrators to manually block/unblock IP addresses
- `BlockResultDto`, `BlockIpRequest`, and `IsBlocked` query endpoints
- Fixed blocklist repository tracking conflict for concurrent delete operations

**Milestone:** The gateway automatically blocks malicious IPs based on threat scores and gives administrators manual block/unblock controls.

## Phase 12 — Dashboard ✅

- `IDashboardService` and `DashboardController` for aggregating security metrics
- Overview endpoints: total requests, blocked requests, active blocks, events today, applications, devices, users
- Security event time-series charts by severity
- Top threats table by threat score
- Top attack types pie chart
- Recent event feed table
- Security timeline chart
- React frontend dashboard using `recharts`, with routing via `react-router-dom`
- Admin-only dashboard endpoints

**Milestone:** Administrators have a visual security dashboard with statistics, charts, real-time event feed, and timeline.

## Phase 13 — Global Map ✅

- `IMapService` and `MapController` for GeoIP-enabled IP data
- Endpoints: map points, attack points, IP details, country list
- Filters: date range, country code, minimum threat score, attacks only, blocked only
- React map page using `leaflet` with OpenStreetMap tiles
- Threat markers colored by score and attack markers with popups
- IP explorer page for searching and viewing detailed IP intelligence
- Admin-only map and IP explorer endpoints

**Milestone:** Administrators can visualize threats and attacks on a world map and inspect detailed GeoIP and reputation data for any IP.

## Phase 14 — Notifications ✅

- `NotificationChannel` and `NotificationLog` entities
- `INotificationChannelProvider` abstraction with pluggable providers
- Providers implemented:
  - Email via existing `IEmailService` / SMTP
  - Telegram via Bot API
  - Discord via webhooks
  - ntfy via HTTP POST
  - WebPush stub validating VAPID keys (full push delivery deferred)
- `INotificationService` for admin CRUD, test sends, and recent logs
- `INotificationDispatcher` integrated into `ThreatDetectionService` for High/Critical security events
- `NotificationsController` for administrators to manage channels and logs
- EF Core migration `AddNotifications`

**Milestone:** Administrators can configure multiple notification channels and receive alerts for high-severity security events.

## Phase 15 — Audit ✅

- `AuditLog` entity with category, action, user, IP address, details, and success status
- `IAuditService` and `AuditService` for writing and searching audit logs
- `AuditController` with admin-only search endpoint and filters:
  - category, action, username, IP address, success, date range
  - pagination via skip/take and total count
- Audit logging integrated into:
  - Authentication: register, login success/failure, logout, password change
  - Access control: trusted network create/update/delete, blocklist create/update/delete
  - Blocking: IP block and unblock
  - Notifications: channel create/update/delete
- EF Core migration `AddAuditLog`

**Milestone:** The gateway records a searchable audit trail of security-relevant administrative and authentication actions.

## Phase 16 — Production Hardening ✅

- `SecurityHeadersMiddleware` adding X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, CSP, Permissions-Policy
- `ForwardedHeadersSettings` and `HstsOptions` with configuration-bound middleware
- Hardened Dockerfiles (non-root users, minimal Alpine images)
- Hardened `docker-compose.yml`:
  - Non-root users, read-only root filesystems, tmpfs mounts
  - `no-new-privileges`, dropped capabilities, resource limits
  - Health checks and dependency conditions
- Postgres backup (`scripts/backup-postgres.sh`) and restore (`scripts/restore-postgres.sh`) scripts
- Fail-closed validation in `GatewayMiddleware` when no upstream URL is configured
- Unit and integration tests for security headers and fail-closed behavior

**Milestone:** The deployment is hardened with security headers, least-privilege containers, resource limits, backups, and explicit fail-closed behavior.

## Phase 17 — V3 Advanced Security ✅

- `IThreatIntelligenceProvider` abstraction and composite `IThreatIntelligenceService`
- AbuseIPDB provider for external IP reputation lookups
- Threat intelligence integrated into IP enrichment (raises threat score from external sources)
- `IBehavioralAnalysisService` with request-burst detection
- `ICrowdSecClient` stub for future CrowdSec local API integration
- WebAuthn/Passkey domain model (`WebAuthnCredential`) and service stub
- `ThreatIntelligenceController`, `BehavioralAnalysisController`, and `WebAuthnController`
- EF Core migration `AddWebAuthnCredentials`

**Milestone:** The gateway can consume external threat intelligence, detect behavioral anomalies, and has a foundation for WebAuthn passkeys and CrowdSec integration.

## Phase 18 — Cloudflare ✅

- `CloudflareOptions` configuration (`SecurityGateway:Cloudflare`)
- `ICloudflareIpService` and `CloudflareIpService` with built-in IPv4 ranges
- `CloudflareClientIpResolver` decorator for trusted Cloudflare proxy IP extraction (`CF-Connecting-IP`, `CF-Visitor-IP`)
- Cloudflare header capture in `GatewayMiddleware.BuildClientIpContext`
- Cloudflare-aware application policy fields: allowed/blocked Cloudflare countries and authentication bypass paths
- Policy evaluation uses `CF-IPCountry` and request path for Cloudflare-aware rules
- `CloudflareController` admin endpoints: status, refresh, IP check
- EF Core migration `AddCloudflarePolicyFields`
- Unit tests for `CloudflareIpService` and `CloudflareClientIpResolver`

**Milestone:** The gateway correctly identifies Cloudflare-proxied traffic, restores the real client IP from Cloudflare headers, and enforces Cloudflare-specific application policies.

## Phase 19 — Access Approval Workflow ✅

- `AccessRequest` and `TrustRecord` entities for explicit access grants
- `AccessRequestStatus`, `AccessRequestDecision`, `ApprovalScope`, and `TrustScope` enums
- `IAccessRequestService` / `AccessRequestService` with `Allow`, `Challenge`, `Deny`, and `Block` decisions
- `GatewayMiddleware` integration:
  - Creates/reuses pending access requests
  - Returns a user-facing challenge page with request ID and polling
  - Sets `sg_session` correlation cookie
  - Allows approved sessions through and blocks denied/blocked requests
- `AccessRequestsController` with admin-only pending/recent/status/resolve endpoints
- Trusted-admin context enforcement on approval endpoints
- `INotificationDispatcher` integration for new pending access requests
- React `ApprovalsPage` with Approve/Deny/Block IP/Block Device actions and scope selection
- Backend unit test for challenge-page behavior and session cookie
- End-to-end production verification: challenged → approved → allowed → new session challenged again
- EF Core migration `AddAccessRequests`

**Milestone:** Unauthenticated or untrusted users see a challenge page, administrators can approve/deny/block requests with scoped trust, and the gateway enforces the resulting access decision.

## Phase 20 — Management UI, CloudflareMode & Inline WAF ✅

- React management pages wired into the admin dashboard:
  - `ApplicationsPage`: CRUD, enable/disable, inline policy editor
  - `TrustedNetworksPage`: CIDR-based trusted networks for admin approvals
  - `DevicesPage`: trust/untrust/block/remove own devices
  - `NotificationsPage`: notification channel CRUD with JSON config and test sends
  - `SecurityEventsPage`: filterable security event table
  - `BlockIpPage`: manual IP block/unblock/check
  - `AuditPage`: searchable, paginated audit log
- `ApplicationCloudflareMode` enum (`Proxied` / `Direct`) added to `Application`
- Backend support to create, update, and persist per-application `CloudflareMode`
- Frontend selector in `ApplicationsPage`
- EF Core migration `AddCloudflareMode`
- `InlineWafMiddleware` with regex-based SQLi, XSS, and path-traversal detection
- WAF event ingestion into `WafEventService` with attack classification
- Configurable via `InlineWaf__Enabled` and `InlineWaf__LogOnly`
- Production nginx hardening:
  - Security headers (HSTS, CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, X-Robots-Tag)
  - `Cache-Control: no-store` on the admin SPA shell
  - Authenticated Origin Pulls already enforced
- Frontend container hardening: runs as non-root `nginx` user
- `docker-compose.yml` updated to expose InlineWaf settings and remove frontend `user: root`

**Milestone:** Administrators can manage applications, devices, networks, notifications, events, blocks, and audit logs from the UI; each app can declare a Cloudflare mode; and the gateway blocks common web attacks inline while logging them as WAF events.

## Phase 21 — WebAuthn / Passkey Authentication

- Replace `IWebAuthnService` stub with a FIDO2 implementation (e.g., `Fido2` library)
- Server-side credential registration ceremony with challenge generation and attestation verification
- Server-side authentication/assertion ceremony with signature verification and sign-count replay protection
- Extend `WebAuthnCredential` entity with attestation, transports, `IsBackupEligible`, and `IsBackupDevice`
- Add `WebAuthnController` endpoints for registration options, register, assertion options, and assert
- React passkey registration and login UI flows
- Admin page to view and revoke user credentials
- Prefer passkeys over passwords; allow passwordless + MFA combinations
- EF Core migration `ExtendWebAuthnCredentials`

**Milestone:** Users can register and authenticate with platform/roaming passkeys, and administrators can manage credentials.

## Phase 22 — CrowdSec Local API Integration

- Implement `ICrowdSecClient` against the CrowdSec Local API (`/v1/decisions`, `/v1/alerts`)
- Background decision-sync worker that polls for new bans/unbans and applies them to the blocklist
- Report high-confidence gateway observations (brute force, WAF blocks, behavioral anomalies) as CrowdSec alerts
- Configurable `CrowdSecOptions`: base URL, API key, TLS, scopes, enable/disable
- Treat CrowdSec decisions as an additional IP reputation source in threat scoring
- Security events/alerts for CrowdSec sync actions
- Integration tests with a CrowdSec testcontainer or a fake local API

**Milestone:** Security Gateway consumes CrowdSec decisions and contributes observations, tightening shared threat intelligence.

## Phase 23 — OIDC / SSO Authentication

- Add ASP.NET Core OpenID Connect authentication scheme
- `IdentityProvider` entity with name, authority, client ID/secret, scopes, and claim mappings
- `IdentityProvidersController` for admin CRUD
- Login UI with "Sign in with ..." options
- Link external identities to local users or enable JIT provisioning
- Map external claims to `UserRole`, email verification, and session device
- Support domain-restricted providers (e.g., only allow a specific email domain)

**Milestone:** Users can authenticate through external OIDC providers, and administrators can configure providers from the dashboard.

## Phase 24 — API Keys & Service Accounts

- `ServiceAccount` and `ApiKey` domain entities with hashed key secrets and expiration
- API key authentication handler/middleware (Bearer or custom header scheme)
- Scoped permissions: per-application read/write or global admin
- `ServiceAccountsController` for admin CRUD and key rotation
- React service-account management page
- Audit logging for all API-key authentication and authorization actions
- Rate limiting applies separately to API-key traffic

**Milestone:** Automated clients and integrations authenticate with scoped, auditable API keys.
