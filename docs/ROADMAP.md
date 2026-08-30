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

- Device enrollment
- Device identity signals
- Device credentials
- Fingerprinting tolerance
- Device/user relationships
- Device management API

## Phase 5 — IP Intelligence

- IP tracking
- GeoIP abstraction
- ASN/ISP lookup
- VPN/proxy/Tor detection abstraction
- Reputation provider abstraction

## Phase 6 — Access Control

- Unknown device challenge
- Approval/denial workflow
- Trusted devices and trusted networks
- Blocking workflow

## Phase 7 — Application Policies

- Domain configuration
- Per-application security policies
- Per-application rules

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
