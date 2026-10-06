# Security Gateway repair deployment gate (2026-10-06)

This is a **pre-deployment checklist**, not authorization to deploy.

## Verified
- Repair branch: `fix/approval-concurrency-20261006` at `2825390`.
- Backend: 192/192 tests passed, including PostgreSQL Testcontainers tests.
- Frontend: 7/7 tests passed; production build succeeded (bundle-size warning).
- Existing Unraid Compose Manager project: `/boot/config/plugins/compose.manager/projects/Security-Gateway`; combined Compose config validated.
- Seven existing production containers running; all seven have `cpuset=1`.
- Existing baseline backups: `/mnt/apps/appdata/security-gateway/backups/repair-baseline-20261006/`, including `postgres.dump`, Compose YAML and pinned image identifiers.
- Production currently uses pinned GHCR image digests for backend/frontend; the source checkout is **not** what production is running.

## Blocking release gates
1. Review all repair-branch changes and complete a staging browser login → authenticated challenge → admin approval → access test. The targeted service/UI tests do not prove end-to-end cookie and host routing behavior.
2. Test the expired-request worker with production-like load; it processes up to 500 requests per five-minute interval. Verify backlog drain rate and DB load.
3. Verify trust-record authorization, idempotency under concurrent approvals, gateway admin isolation, Cloudflare client IP resolution, WAF detection-only, rate limits, and Immich bypass.
4. Decide how device activity telemetry should be updated without writing the device row on every request.
5. Build and publish **immutable, reviewable** backend and frontend image digests from the approved commit. Do not point Compose at mutable `latest` tags.
6. Take a fresh, restorable PostgreSQL backup immediately before deployment; validate backup integrity and store image IDs and both current Compose files.
7. Review Compose differences **against the actual Unraid Compose Manager files**, not `docker-compose.prod.yml` in the repo (the live project includes a WAF log forwarder and a different gateway image).
8. Arrange a maintenance window and explicitly approve production deployment.

## Controlled deployment procedure (after approval)
1. Record current seven container IDs, health, image digests, CPU affinity, active routes, and Compose config.
2. Preserve existing appdata volumes, TLS material, NPM data, WAF configuration, and Immich bypass.
3. Update **only** backend/frontend image digests in the Compose Manager project after reviewing the rendered diff. Keep `cpuset: '1'` for all seven services.
4. Use Compose Manager to recreate only services whose images changed; do not run `docker compose down -v` or recreate PostgreSQL/Redis/NPM unnecessarily.
5. Verify health, sign-in, challenge polling, approval, trusted-device access, WAF detection-only, public routes, Immich bypass, rate limiting, and CPU affinity.
6. Watch logs for HTTP 500, EF concurrency exceptions, expired-request cleanup failures, and increased 403/502 responses.

## Rollback
1. Restore the **previous pinned image digests** and original Compose YAML/override from the fresh pre-deployment backup, then recreate changed services via Compose Manager.
2. Revalidate all routes, health checks, CPU pinning, NPM access, and WAF behavior.
3. **Database warning:** reverting an image is not equivalent to reverting a database migration. Review applied migrations before rollback. Restore a PostgreSQL dump only when necessary and only with a documented data-loss/maintenance decision.
4. Keep logs and a short incident timeline. Never remove existing appdata as part of rollback.

## Status
**NOT DEPLOYED.** These instructions do not establish readiness for production until the release gates above are completed.
