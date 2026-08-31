# ADR-019 — Security Dashboard

## Status

Accepted

## Context

With security events, WAF events, rate limiting, threat scores, and blocklist data flowing through the gateway, administrators need a single place to understand the security posture of their deployment. We needed a read-only aggregation layer and a frontend that can display statistics, charts, and recent events without duplicating business logic.

## Decision

We created a dashboard feature composed of a backend aggregation service and a React dashboard page.

Key design points:

- **Backend aggregation**: `IDashboardService` implemented by `DashboardService` aggregates data from existing `ApplicationDbContext` sets (`IpAddresses`, `SecurityEvents`, `WafEvents`, `BlocklistEntries`, `Applications`, `Devices`, `Users`). It returns DTOs optimized for the UI.
- **Admin-only API**: `DashboardController` exposes endpoints under `/api/dashboard` restricted to the `Administrator` role.
- **Frontend charts**: The dashboard uses `recharts` for bar charts, pie charts, and timelines. This keeps chart configuration declarative and React-friendly.
- **Routing**: `react-router-dom` was added so the existing health page remains available at `/health` while the dashboard is served at `/`.
- **Polling-based real-time feed**: The recent events table loads on mount. Future iterations can add Server-Sent Events or WebSockets for true real-time updates.
- **No new database entities**: The dashboard is read-only and reuses existing tables, avoiding migrations and keeping it lightweight.

## Consequences

- **Pros**:
  - Centralized security visibility for administrators.
  - Reuses existing domain model and infrastructure.
  - Declarative chart components are easy to extend.

- **Cons**:
  - Dashboard queries run against the primary database; high-traffic deployments may need materialized views or caching.
  - "Real-time" is currently polling-based; live updates require additional work.

## Alternatives Considered

- **Separate analytics/OLAP database**: Rejected for Phase 12 due to operational complexity.
- **Server-Sent Events for real-time updates**: Deferred to a future iteration to keep scope manageable.
