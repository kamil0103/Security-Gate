# ADR-023 — Production Hardening

## Status

Accepted

## Context

As the gateway approaches production use, it needed hardening across HTTP response security, container runtime security, reverse proxy header handling, backups, and fail-closed behavior.

## Decision

We implemented a set of production hardening measures.

Key design points:

- **Security headers middleware**: A custom `SecurityHeadersMiddleware` adds `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Content-Security-Policy`, and `Permissions-Policy` to all responses.
- **HSTS and forwarded headers**: `HstsOptions` and `ForwardedHeadersSettings` are bound to configuration. `UseHsts` runs outside development, and `UseForwardedHeaders` runs when enabled so the gateway correctly resolves client IPs and schemes behind a reverse proxy.
- **Container hardening**:
  - Backend Dockerfile creates a non-root `appuser` and switches to it.
  - Frontend Dockerfile switches to the `nginx` user and listens on port 8080 (unprivileged).
  - `docker-compose.yml` enforces non-root users, `read_only: true`, `tmpfs` for writable directories, `security_opt: no-new-privileges`, dropped capabilities, and memory/CPU limits.
- **Backup and recovery**: `scripts/backup-postgres.sh` and `scripts/restore-postgres.sh` use `pg_dump`/`psql` against the running Postgres container.
- **Fail-closed validation**: `GatewayMiddleware` now falls back to the configured default upstream URL and returns `502 Bad Gateway` if no upstream is configured, preventing accidental open proxying.
- **Test coverage**: Added unit tests for the security headers middleware, integration tests verifying headers on the health endpoint, and a fail-closed test for the gateway middleware.

## Consequences

- **Pros**:
  - Reduces attack surface via defense-in-depth headers and least-privilege containers.
  - Provides operational backup/restore scripts.
  - Explicit fail-closed behavior prevents accidental misconfiguration exposure.

- **Cons**:
  - Read-only root filesystems may require additional `tmpfs` mounts as the application evolves.
  - Running nginx on port 8080 requires explicit port mapping changes.
  - HSTS preload is disabled by default and must be explicitly enabled.

## Alternatives Considered

- **Use a WAF/mod_security in front of the gateway**: Already referenced as the `modsecurity-crs` service; the headers provide baseline defense.
- **Cloud-native secret management**: Deferred; secrets remain environment variables for self-hosted simplicity.
