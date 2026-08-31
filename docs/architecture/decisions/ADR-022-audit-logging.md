# ADR-022 — Audit Logging

## Status

Accepted

## Context

The gateway performs many security-sensitive actions: authentication, access control changes, blocking, and notification configuration. Administrators and compliance requirements demand a searchable history of who did what, when, from where, and whether it succeeded.

## Decision

We introduced a centralized audit logging subsystem.

Key design points:

- **Domain model**: `AuditLog` captures timestamp, category, action, user ID, username, source IP, details, and success flag.
- **Service abstraction**: `IAuditService` provides `LogAsync` and `SearchAsync`/`CountAsync` methods. `AuditService` writes logs immediately and supports filtering.
- **Repository**: `IAuditLogRepository` / `AuditLogRepository` implement search with filters for category, action, username, IP address, success, and date range, plus pagination.
- **Controller**: `AuditController` exposes an admin-only search endpoint returning total count and paged logs.
- **Integration**: Audit calls were added to key services after successful (or failed) operations:
  - `AuthenticationService`: register, login, login failure, logout, password change
  - `AccessControlService`: trusted network and blocklist CRUD
  - `AutomaticBlockingService`: block and unblock
  - `NotificationService`: channel CRUD
- **No outbox**: Logs are written synchronously for simplicity. Future iterations can add background processing if needed.

## Consequences

- **Pros**:
  - Single, queryable audit trail.
  - Easy to extend to additional actions.
  - Filters and pagination support large log volumes.

- **Cons**:
  - Synchronous writes add latency to audited operations.
  - No log retention or archiving policy yet.
  - User ID/username must be passed explicitly by services; a security context abstraction would reduce boilerplate.

## Alternatives Considered

- **Event-driven audit with middleware**: Rejected because many audited actions happen inside services without a clear HTTP context.
- **Per-domain audit tables**: Rejected in favor of a unified log for simpler search and reporting.
