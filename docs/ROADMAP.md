# Security Gateway — Roadmap

## Phase 0 — Project Setup

- [x] Repository initialized
- [ ] Documentation: README, spec, roadmap, start here, contributing, security
- [ ] Architecture Decision Records
- [ ] GitHub Actions CI scaffolding
- [ ] Branching and commit conventions documented

**Milestone:** Repository is ready for development.

## Phase 1 — Development Infrastructure

- [ ] React 19 + TypeScript + Vite frontend
- [ ] ASP.NET Core 9 backend with Clean Architecture
- [ ] PostgreSQL 16 in Docker Compose
- [ ] Redis 7 in Docker Compose
- [ ] Backend health endpoint verifying Postgres and Redis connectivity
- [ ] Frontend health page communicating with backend
- [ ] Docker Compose development environment
- [ ] Backend integration tests
- [ ] Frontend smoke tests
- [ ] CI pipeline passing

**Milestone:** Every developer can clone the repository and run the entire development environment.

## Phase 2 — Gateway Foundation

- Gateway networking and request handling
- Trusted proxy chain configuration
- Client IP extraction and validation
- NPM communication
- Logging
- Fail-closed networking design

## Phase 3 — Authentication

- Local accounts
- Login/logout
- Sessions (expiration, revocation)
- Password change and reset
- Email verification
- SMTP configuration

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
