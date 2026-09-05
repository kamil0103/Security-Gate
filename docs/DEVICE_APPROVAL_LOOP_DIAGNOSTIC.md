# Security Gateway — Device Approval Loop Diagnostic

## Executive Summary

The Security Gateway repeatedly challenges already-approved devices because the server-side `TrustRecord` created by an administrator approval is over-scoped: it stores and then requires the ephemeral `SessionId` (and `ClientIp`) even when the administrator chose a broader approval scope such as **Device**, **Permanent**, or **IPAndDevice**. When the browser presents a new session identifier on a subsequent request—which we observed in production for the same physical browser/IP pair—the `TrustRecord` no longer matches, so the gateway treats the request as a brand-new unapproved device.

This was reproduced at the source level and verified against the live database state. A direct browser reproduction on the deployed build is partially blocked by an unrelated older build bug (unquoted `publicId` in the challenge-page JavaScript), but the cookie/session behavior and database evidence prove the identity mismatch.

## Reproduction

### Phase 1 — Runtime evidence from the deployed system

Target: `192.168.5.184` (Unraid host, `security-gateway-backend:trusted-ip`).

Using direct `curl` through the gateway nginx to the backend container:

```bash
docker exec security-gateway-nginx curl -s -i \
  -H 'Host: sonarr.toncom159.com' \
  -H 'User-Agent: TestBrowser/1.0' \
  http://security-gateway-backend:8080/
```

Result:
- HTTP 200 challenge page.
- `Set-Cookie: sg_session=<guid>; domain=sonarr.toncom159.com; path=/; secure; samesite=strict; httponly`.

When the cookie is sent back on a second request, the backend recognizes the session and does **not** issue a new cookie. This proves the backend session-cookie logic works when the cookie is actually transmitted.

Using Playwright against the public `https://sonarr.toncom159.com/`:

| Scenario | Result |
|---|---|
| Chromium default | Cookie persists; after admin approval, refresh is allowed. |
| Firefox default | Cookie persists; after admin approval, refresh is allowed. |
| Firefox `network.cookie.cookieBehavior=2` (Strict) | `sg_session` cookie rejected; `localStorage` blocked; every request gets a new session. |

The exact CachyOS desktop configuration could not be reproduced locally, but the production database records (below) show the same symptom: the same browser/IP pair creates multiple `AccessRequest` rows with different `SessionId` values.

### Phase 1 — Database evidence

```sql
SELECT "PublicId", "ClientIp", "DeviceFingerprint", "SessionId", "Status", "RequestCount"
FROM "AccessRequests"
WHERE "ClientIp" = '47.145.35.238'
ORDER BY "CreatedAt" DESC;
```

Excerpt:

| PublicId | ClientIp | Fingerprint | SessionId | Status | RequestCount |
|---|---|---|---|---|---|
| TCK2UXG2GE | 47.145.35.238 | 23483336219B53FC8875C9AC1DC2706EC3B47DC962AE18FA254F089FABEB77EC | 69b71a937d4c400b9f5836c737e744b8 | Pending | 1 |
| 3KTV59DDAB | 47.145.35.238 | 23483336219B53FC8875C9AC1DC2706EC3B47DC962AE18FA254F089FABEB77EC | 4ad98b5715254e6598f6cb73080b959f | Pending | 2 |
| QMXEXRZG6F | 47.145.35.238 | 23483336219B53FC8875C9AC1DC2706EC3B47DC962AE18FA254F089FABEB77EC | b3feac639ec64824a389cef09d20858f | Pending | 1 |

Same browser (Firefox on Linux, same User-Agent hash), same public IP, but a different `SessionId` per request. The `RequestCount=2` row shows that the cookie sometimes persists for two requests, but then a new session appears.

Corresponding `TrustRecords` for the Chrome/Linux browser on the same IP:

| Scope | ClientIp | Fingerprint | SessionId | AccessRequest |
|---|---|---|---|---|
| IpAndDevice | 47.145.35.238 | 7484A43ED9A5CDFE5C0BEA134BC7FE95BC75739359F54E82E7B03A349F66B619 | 7b5b8129aaf84477bbe12777ad8e4fb7 | BDGMVHSHU6 |
| Permanent | 47.145.35.238 | 7484A43ED9A5CDFE5C0BEA134BC7FE95BC75739359F54E82E7B03A349F66B619 | 31327c0a16dc45d699cb60c904f088fa | C8XK6BGQ95 |
| Permanent | 47.145.35.238 | 7484A43ED9A5CDFE5C0BEA134BC7FE95BC75739359F54E82E7B03A349F66B619 | 6850e0e0aae8414f84055570d00ee817 | 59WH8RN69S |

The same physical browser required **three separate Permanent approvals** because each approval was bound to a different `SessionId`.

## Identity Model

The gateway currently recognizes a request through the following chain:

1. `ClientIpResolver` extracts the client IP from `CF-Connecting-IP`, `X-Forwarded-For`, or the socket IP.
2. `GatewayMiddleware.GetOrCreateSessionId` reads the `sg_session` cookie or generates a new GUID.
3. `GatewayMiddleware.ComputeFingerprint` hashes the `User-Agent` string (SHA256).
4. `AccessRequestService.EvaluateAccessAsync`:
   1. Queries active `TrustRecord`s by `ApplicationId` + `ClientIp` + `DeviceFingerprint` + `UserId` + `SessionId`.
   2. For authenticated users, also queries the `Device` table by `UserId` + `Fingerprint`.
   3. If no trust is found, creates an `AccessRequest` and returns `Challenge`.
5. Administrator approval calls `AccessRequestService.ResolveAsync`, which creates a `TrustRecord` from the `AccessRequest` values.

The intended trust scopes are:
- `Session` — trust lasts for the browser session.
- `Device` — trust lasts for the browser/device fingerprint.
- `IpAndDevice` — trust lasts for the combination of IP and device fingerprint.
- `Ip` — trust lasts for the IP address.
- `Permanent` — trust does not expire.

## Before/After Identity Comparison

For the Chrome/Linux browser on `47.145.35.238`:

| Identity component | Before Approval | After Approval (Permanent) | Same? |
|---|---|---|---|
| Client IP | 47.145.35.238 | 47.145.35.238 | Yes |
| Device fingerprint | 7484A43ED9A5CDFE5C0BEA134BC7FE95BC75739359F54E82E7B03A349F66B619 | 7484A43ED9A5CDFE5C0BEA134BC7FE95BC75739359F54E82E7B03A349F66B619 | Yes |
| Session identifier | 31327c0a16dc45d699cb60c904f088fa | 31327c0a16dc45d699cb60c904f088fa | Yes (at approval time) |
| AccessRequest status | Pending | Approved | Changed |

Next request from the same browser:

| Identity component | Approved Record | Next Request | Same? |
|---|---|---|---|
| Client IP | 47.145.35.238 | 47.145.35.238 | Yes |
| Device fingerprint | 7484A... | 7484A... | Yes |
| Session identifier | 31327... | 1eea5001ca924c4887b8770edfed07c0 | **No** |
| TrustRecord match? | — | — | **No** |
| Result | — | New `AccessRequest` created | Loop |

The session identifier is the unstable component. Because the `TrustRecord` was written with `SessionId = '31327...'`, a subsequent request with a different session cannot match even though the IP and fingerprint are identical.

## Database State

- `AccessRequests` contains many rows for the same `ClientIp` + `DeviceFingerprint` with different `SessionId`s.
- `TrustRecords` for non-Session scopes also contain distinct `SessionId`s, proving the scope is not being honored.
- The `TrustRecords.ClientIp` column is `NOT NULL`, so even a `Permanent` or `Device` approval cannot be stored without binding it to a specific IP.

## Approval State Transition

`AccessRequestService.ResolveAsync` maps the admin's `ApprovalScope` to a `TrustScope` and expiration, but then copies **all** identity fields from the `AccessRequest` into the `TrustRecord`:

```csharp
var trust = new TrustRecord
{
    Scope = scope,
    ApplicationId = request.ApplicationId,
    ClientIp = request.ClientIp,            // always set
    DeviceFingerprint = request.DeviceFingerprint,
    UserId = request.UserId,
    SessionId = request.SessionId,          // always set
    ...
};
```

Consequently:
- A `Permanent` approval is still bound to the current `SessionId`.
- A `Device` approval is still bound to the current `ClientIp` and `SessionId`.
- An `Ip` approval is still bound to the current `SessionId`.

## Challenge/Continue Flow

The deployed build renders invalid JavaScript (`const publicId = ABCD1234;` without quotes), so the challenge page cannot poll or enable the Continue button. The user must refresh manually. On refresh, if the session cookie changed, a new `AccessRequest` is created and the loop continues.

The current source branch (`race-repair-clean`) already fixes the JavaScript quoting with `JsonEncodedText.Encode`, but the underlying identity mismatch remains.

## Cookie Analysis

- Cookie name: `sg_session`
- Domain: request host (e.g., `sonarr.toncom159.com`)
- Path: `/`
- Secure: `true`
- HttpOnly: `true`
- SameSite: `Strict`
- Expires: 1 day

The cookie is correctly scoped. When it is transmitted, the backend recognizes it. The observed session changes are therefore not caused by a domain/path mismatch; they are caused by the browser not sending the same cookie value, or by the `TrustRecord` being bound to a single cookie value.

## Browser Storage Analysis

The challenge page currently does not use `localStorage` or `sessionStorage` for any device identifier. The only persistence mechanism is the `sg_session` cookie. If that cookie is lost or rejected, the gateway has no correlation signal other than the `User-Agent` hash.

## Fingerprint Stability

`GatewayMiddleware.ComputeFingerprint` returns `SHA256(User-Agent)`. It does **not** use the `sessionId` parameter despite accepting it. For a fixed browser version, the fingerprint is stable. For different browsers or browser updates, it changes. The fingerprint is a risk signal, not a hardware identifier.

## IP Stability

Production records show the same public IP for multiple requests from the same browser. However, the `TrustRecord` lookup always filters by `ClientIp`, so even an intentional IP change (e.g., IPv6 privacy address rotation, VPN, mobile handoff) would break a `Device` or `Permanent` approval. This is inconsistent with the documented trust scopes.

## Cache Analysis

No `TrustRecord` or device trust caching was found in the request path. The database is queried on every gateway request.

## Root Cause

**VERIFIED ROOT CAUSE**

The `TrustRecord` created on approval is over-scoped. `AccessRequestService.CreateTrustRecordAsync` copies the ephemeral `SessionId` and `ClientIp` into the record regardless of the selected `ApprovalScope`, and `TrustRecordRepository.FindActiveAsync` always requires an exact `ClientIp` match. Therefore:

1. A `Device`, `Permanent`, or `IPAndDevice` approval is accidentally bound to the single session that submitted the request.
2. A `Device` or `Permanent` approval is accidentally bound to the IP address at the time of approval.
3. When the browser sends a new session identifier (cookie refresh, strict privacy mode, new tab behavior, etc.), the `TrustRecord` does not match.
4. The gateway creates a new `AccessRequest`, producing the observed infinite approval loop.

## Repair

Implemented in branch `race-repair-clean` and deployed as `security-gateway-backend:device-approval-fix` on `192.168.5.184`.

### Backend identity fix

1. **`TrustRecord.ClientIp` made nullable** (`TrustRecord.cs`).
2. **`AccessRequestService.ResolveAsync` / `CreateTrustRecordAsync`** now call `ResolveTrustRecordIdentity` so only scope-relevant identity fields are persisted:
   - `Session`: `ClientIp`, `SessionId`, `DeviceFingerprint`
   - `Device`: `DeviceFingerprint` only
   - `IpAndDevice`: `ClientIp` + `DeviceFingerprint`
   - `Ip`: `ClientIp` only
   - `Permanent`: `DeviceFingerprint` only
3. **`TrustRecordRepository.FindActiveAsync`** now uses nullable matching: a null column matches any value, so a `Device`/`Permanent` record with null `ClientIp`/`SessionId` is found regardless of the current session or IP.
4. Added EF Core migration `20260905180000_TrustRecordScopeIdentity` to make `TrustRecords.ClientIp` nullable.

### Challenge-page fixes

1. **JavaScript quoting** in `GatewayMiddleware.cs`: replaced `JsonEncodedText.Encode(...).ToString()` with `JsonSerializer.Serialize(...)` so the challenge page renders `const publicId = "ABC123";` instead of the invalid `const publicId = ABC123;`.
2. **Polling endpoint bypass**: added an early `_next()` for paths starting with `/api/access-requests/` so the approval-status polling reaches the backend controller instead of being proxied to the upstream application.

### Tests

- `backend/tests/SecurityGateway.Tests/AccessControl/AccessRequestServiceApprovalTests.cs`: 9 unit tests covering scope-aware trust creation and lookup.
- `backend/tests/SecurityGateway.Tests/Gateway/GatewayMiddlewareTests.cs`: added regression tests for JS quoting and `/api/access-requests/` bypass.

### Runtime verification

- `Device`, `Permanent`, and `IpAndDevice` approvals now grant access after clearing the session cookie.
- `Session` approvals correctly require a new challenge after cookie clear.
- Full Chromium and Firefox flows (challenge → admin approval → polling enables Continue → click Continue → refresh 5× → new tab) succeed without generating duplicate `AccessRequest` rows.
- `TrustRecords` created after the fix show `ClientIp` and `SessionId` as `NULL` for `Device`/`Permanent` scopes.
