# Security Gateway — Reverse-Proxy Compatibility Diagnostic

**Date:** 2026-09-02  
**Target system:** 192.168.5.184 (Unraid host, root access used for read-only inspection)  
**Repository:** `https://github.com/kamil0103/Security-Gate`  
**Status:** No changes were made to the system. This document is diagnostic only.

---

## 1. Executive Summary

The application compatibility problems are **not** caused by CSP, nginx path rewriting, `/api` mis-routing, cookies, authentication, firewall, Cloudflare, or NPM proxy configuration. The confirmed root cause is a **dependency-injection lifetime bug in the Security Gateway backend**: `GatewayMiddleware` (and `InlineWafMiddleware`) capture scoped EF Core / `DbContext` services in their constructors, which makes the middleware instance a de-facto singleton sharing one `DbContext` across concurrent requests. Under the parallel static-asset/API load produced by Sonarr, Radarr, and qBittorrent, this causes `InvalidOperationException: A second operation was started on this context instance before a previous operation completed`, and Kestrel returns **HTTP 500 with an empty body**.

Because a modern web UI loads dozens of CSS/JS/API resources in parallel, a non-deterministic subset of those requests fail. The visible symptoms are:

* Sonarr / Radarr: the app shell loads, but one or more translation/API requests return 500, producing `Failed to load translations from API`.
* qBittorrent: after login, many stylesheets and scripts return 500 with no `Content-Type`, so the UI renders unstyled and partially broken.
* `form.riskshields.com`: also affected under concurrent load, although it normally works because it is not registered as a gateway `Application` and therefore receives no challenge/approval traffic.

The fix is a source-code change to the backend middleware to resolve scoped services per request (via `IServiceScopeFactory` or `InvokeAsync` parameters), followed by a rebuilt Docker image. No security control needs to be weakened.

---

## 2. Sonarr

### 2.1 Routing

```
Browser/Cloudflare
  → security-gateway-nginx (443 default_server, Cloudflare origin cert + client cert)
  → security-gateway-backend:8080 (GatewayMiddleware)
  → security-gateway-nginx-proxy-manager:80 (Host: sonarr.toncom159.com)
  → 192.168.5.184:8989 (sonarr container)
```

`sonarr.toncom159.com` is registered in the gateway `Applications` table with `RequireAuthentication = true`, so the gateway challenges unapproved clients and requires an approved session before proxying.

### 2.2 Exact failing request(s)

The translation error maps to the Sonarr frontend fetching localisation data. The relevant endpoints are:

* `GET https://sonarr.toncom159.com/api/v3/localization` — the full translation string table.
* `GET https://sonarr.toncom159.com/api/v3/localization/language` — available UI languages.

Runtime evidence (50 parallel requests, existing approved `sg_session` cookie, matching User-Agent/fingerprint):

```text
HTTP/2 401  ×19   (upstream Sonarr rejects because no Sonarr auth cookie/API key in the probe)
HTTP/2 500  ×31   (Security Gateway backend crashed)
```

A captured 500 response:

```text
HTTP/2 500
content-length: 0
server: cloudflare
content-security-policy: default-src * 'unsafe-inline' 'unsafe-eval' data: blob:; frame-ancestors 'none';
x-content-type-options: nosniff
x-frame-options: SAMEORIGIN
...
```

The response body is empty and the request never reaches Sonarr.

Backend logs at the same moment show the failure is inside the gateway:

```text
System.InvalidOperationException: A second operation was started on this context instance before a previous operation completed.
   at Microsoft.EntityFrameworkCore.Infrastructure.Internal.ConcurrencyDetector.EnterCriticalSection()
   at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()
   at SecurityGateway.Infrastructure.Applications.Services.ApplicationPolicyService.GetApplicationByDomainAsync(...)
   at SecurityGateway.Api.Middleware.GatewayMiddleware.InvokeAsync(...)
```

### 2.3 What was ruled out

* **CSP:** The deployed policy is `default-src * 'unsafe-inline' 'unsafe-eval' data: blob:; frame-ancestors 'none';` — permissive; no browser blocks were observed.
* **`/api` interception:** For `sonarr.toncom159.com`, `/api/v3/*` is proxied to NPM because `IsAdminPath` only matches `admin.toncom159.com`.
* **Path rewriting:** NPM receives `/api/v3/localization` unchanged and routes by `Host`.
* **Host header:** NPM access logs show `Host: sonarr.toncom159.com` and the correct upstream port.

---

## 3. Radarr

### 3.1 Routing

Identical to Sonarr, via `radarr.toncom159.com` → NPM → `192.168.5.184:7878`.

### 3.2 Exact failing request(s)

Radarr version 6.3.0.10514 uses the same *arr UI pattern. The failing translation path is:

* `GET https://radarr.toncom159.com/api/v3/localization` or `GET https://radarr.toncom159.com/api/v3/localization/language`.

Runtime evidence (20 parallel requests with approved `sg_session`):

```text
HTTP/2 401  ×12
HTTP/2 500  ×8
```

Backend logs again show `ApplicationPolicyService.GetApplicationByDomainAsync` failing with the same `DbContext` concurrency exception.

NPM access logs from a real browser session show that successful requests do reach Radarr:

```text
200 200 - GET http radarr.toncom159.com "/api/v3/localization/language"
200 200 - GET http radarr.toncom159.com "/api/v3/config/ui"
200 200 - GET http radarr.toncom159.com "/api/v3/collection"
```

This proves routing works; the failures are intermittent and correlate with concurrency.

### 3.3 What was ruled out

Same as Sonarr: CSP, `/api` interception, path rewriting, and Host header are not the cause.

---

## 4. qBittorrent

### 4.1 Routing

```
Browser/Cloudflare
  → security-gateway-nginx (443 default_server)
  → security-gateway-backend:8080
  → security-gateway-nginx-proxy-manager:80 (Host: qbittorrent.toncom159.com)
  → 192.168.5.184:8080 (gluetun → qbittorrent)
```

### 4.2 Exact failing requests

After authentication, the main WebUI HTML loads but the browser issues parallel requests for assets such as:

* `GET /css/Window.css?v=9mymzr`
* `GET /css/Core.css?v=9mymzr`
* `GET /scripts/cache.js?v=9mymzr`
* `GET /scripts/lib/MooTools-Core-1.6.0-compat-compressed.js`
* `GET /images/torrent-start.svg`

Selenium-driven Chromium (with valid `sg_session` and `QBT_SID` cookies) produced console errors like:

```text
Refused to apply style from '.../css/Window.css?v=9mymzr' because its MIME type ('') is not a supported stylesheet MIME type
Failed to load resource: the server responded with a status of 500 ()  — scripts/cache.js
Failed to load resource: the server responded with a status of 500 ()  — images/torrent-start.svg
Uncaught ReferenceError: Hash is not defined
Uncaught TypeError: Cannot read properties of undefined (reading 'Require')
```

A controlled concurrency test confirms the 500s are gateway-side:

```text
for i in {1..30}; do
  curl -sS -D - -b qbit_cookies.txt -H "Cookie: sg_session=..." \
    "https://qbittorrent.toncom159.com/scripts/cache.js?v=9mymzr&r=$RANDOM$i" ...
done

Result:
HTTP/2 200 ×18
HTTP/2 500 ×12
```

### 4.3 Why it looks unstyled

The failed CSS/JS requests return HTTP 500 with an empty body and no `Content-Type`. The browser therefore refuses to apply the stylesheet or execute the script. The remaining HTML is rendered as plain, unstyled markup.

### 4.4 What was ruled out

* **CSP:** Not blocking; the deployed CSP is permissive.
* **MIME type misconfiguration:** Upstream qBittorrent returns correct `text/css` / `text/javascript` when the request succeeds.
* **Path rewriting:** NPM logs show the original path is preserved and routed correctly.
* **qBittorrent auth:** Login succeeds (`204` from `/api/v2/auth/login`); the issue is on subsequent asset requests.

---

## 5. form.riskshields.com

### 5.1 Independent domain status

`form.riskshields.com` is **not** listed in the gateway `Applications` table:

```text
SELECT "Domain" FROM "Applications";
-- returns immich, cast, home, lifeos, event, sonarr, radarr, qbittorrent, seerr
-- form.riskshields.com is absent
```

The deployed nginx config has a dedicated, non-Cloudflare server block for it:

```nginx
server {
    listen 443 ssl;
    server_name form.riskshields.com;
    ssl_certificate /etc/letsencrypt/live/npm-15-rsa/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/npm-15-rsa/privkey.pem;
    # No ssl_verify_client / Authenticated Origin Pulls
    location / {
        proxy_pass http://backend:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
    }
}
```

### 5.2 Routing

```
Internet
  → security-gateway-nginx (443, Let's Encrypt cert)
  → security-gateway-backend:8080
  → security-gateway-nginx-proxy-manager:80 (Host: form.riskshields.com)
  → 192.168.5.184:3001 (riskshields-frontend)
```

Because it is not a registered `Application`, `GatewayMiddleware` calls `ProxyToUpstreamAsync` with no access-control evaluation. There is no challenge page and no authentication requirement at the gateway.

### 5.3 Verification

A simple public `curl` returns `200 OK`:

```text
HTTP/1.1 200 OK
Server: nginx/1.31.4
Content-Type: text/html
X-Served-By: form.riskshields.com
Content-Security-Policy: default-src * 'unsafe-inline' 'unsafe-eval' data: blob:; frame-ancestors 'none';
```

### 5.4 Concurrent-load behaviour

Although it works under normal single-request use, the same backend concurrency bug triggers 500s when many requests are issued in parallel:

```text
20 parallel GET https://form.riskshields.com/
→ 7 × HTTP/1.1 200 OK
→ 13 × HTTP/1.1 500 Internal Server Error
```

This confirms the root cause is global to the gateway backend, not specific to the *arr apps.

---

## 6. `/api` Routing Analysis

### 6.1 Nginx layer

| Domain | Nginx server block | `/api/*` handling |
|---|---|---|
| `admin.toncom159.com` | Cloudflare, 443, `ssl_verify_client on` | `location /api/ { proxy_pass http://backend:8080; }` — direct to backend controllers |
| `*.toncom159.com` (catch-all) | default_server, Cloudflare, `ssl_verify_client on` | No `/api` location; `location /` sends everything to `backend:8080` |
| `form.riskshields.com`, `drive.riskshields.com` | Direct LE cert | No `/api` location; `location /` sends everything to `backend:8080` |

### 6.2 Backend layer (`GatewayMiddleware.IsAdminPath`)

```csharp
private bool IsAdminPath(string host, string path)
{
    if (!host.Equals(_options.AdminDomain, StringComparison.OrdinalIgnoreCase))
        return false;

    foreach (var prefix in _options.AdminPathPrefixes) // ["/api", "/swagger"]
    {
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return true;
    }
    return false;
}
```

* **Admin domain + `/api/*` or `/swagger/*`:** bypasses the gateway proxy and is handled by backend controllers.
* **Any other domain + `/api/*`:** treated as an upstream application path and proxied to NPM.
* **Admin domain + anything else:** served by the frontend container.

### 6.3 Evidence

* `https://admin.toncom159.com/api/health` returns the backend health JSON directly.
* `https://sonarr.toncom159.com/api/v3/system/status` (with API key) is proxied to Sonarr and returns upstream JSON when the gateway does not crash.
* NPM proxy-host logs for `sonarr.toncom159.com` show requests such as `GET http sonarr.toncom159.com "/api/v3/localization/language"` reaching the upstream.

**Conclusion:** `/api` is correctly distinguished by hostname. This is not the root cause.

---

## 7. CSP Analysis

### 7.1 Who generates the CSP?

The deployed gateway nginx config generates a **permissive default CSP** for every proxied domain:

```nginx
map $host $csp_default {
    default "default-src * 'unsafe-inline' 'unsafe-eval' data: blob:; frame-ancestors 'none';";
    admin.toncom159.com "...strict admin policy...";
}

proxy_hide_header Content-Security-Policy;
add_header Content-Security-Policy $csp_default always;
```

The backend `SecurityHeadersMiddleware` currently running in the deployed image does **not** add a CSP for non-admin hosts (it only adds `X-Frame-Options: SAMEORIGIN`). The uncommitted repository changes would tighten this, but they have not been deployed.

### 7.2 Is CSP blocking resources?

No. A representative response for a successful qBittorrent stylesheet:

```text
content-security-policy: default-src * 'unsafe-inline' 'unsafe-eval' data: blob:; frame-ancestors 'none';
content-type: text/css
```

A failed qBittorrent stylesheet:

```text
content-security-policy: default-src * 'unsafe-inline' 'unsafe-eval' data: blob:; frame-ancestors 'none';
(content-type absent, body empty, status 500)
```

The failures are HTTP 500 from the gateway, not CSP violations. Browser console messages about MIME type are a **symptom** of the empty 500 response, not a cause.

**Conclusion:** CSP is **not responsible** for the compatibility problems.

---

## 8. Cookie Analysis

### 8.1 Gateway session cookie (`sg_session`)

Set by `GatewayMiddleware.EnsureSessionCookie`:

```text
Set-Cookie: sg_session=<guid>; expires=...; domain=<request-host>; path=/; secure; samesite=none; httponly
```

* **Domain:** scoped to the requested host (`sonarr.toncom159.com`, `radarr.toncom159.com`, etc.) — no cross-domain leakage.
* **Secure + HttpOnly:** yes.
* **SameSite:** `None` in the deployed image; the uncommitted repository diff changes it to `Strict`.

### 8.2 Upstream application cookies

The gateway copies upstream `Set-Cookie` headers verbatim. For example, qBittorrent login sets:

```text
Set-Cookie: QBT_SID_8080=...; HttpOnly; expires=...; path=/
```

and it reaches the browser unchanged.

### 8.3 Cookie mixing

No evidence of cookies intended for one application being applied to another. Each app cookie is scoped to its own host.

**Conclusion:** Cookies are **not responsible** for the observed failures.

---

## 9. Header Analysis

### 9.1 Headers sent upstream

Nginx sends to the backend:

```text
Host: <original-host>
X-Real-IP: $remote_addr
X-Forwarded-For: $proxy_add_x_forwarded_for
X-Forwarded-Proto: $scheme
X-Forwarded-Host: $host
```

The backend `HttpClientProxyService` forwards the original `Host` header to NPM:

```csharp
if (!string.IsNullOrWhiteSpace(request.Host))
{
    upstreamRequest.Headers.Host = request.Host;
}
```

NPM proxy-host access logs confirm the upstream application sees the correct hostname, e.g.:

```text
GET http sonarr.toncom159.com "/api/v3/localization/language" [Client 47.145.35.238]
GET http qbittorrent.toncom159.com "/" [Client 47.145.35.238]
GET http form.riskshields.com "/" [Client ...]
```

### 9.2 Origin / Referer

These are passed through untouched. No header rewriting that would break CSRF or form login was observed.

**Conclusion:** Proxy headers are correct and are **not responsible**.

---

## 10. Nginx / NPM Routing

### 10.1 Actual routing table

| Domain | Gateway nginx server block | Gateway backend action | NPM proxy host | Upstream |
|---|---|---|---|---|
| `admin.toncom159.com` | Cloudflare 443 + client cert | Serves admin UI / API controllers directly | n/a | `frontend:8080` / `backend:8080` |
| `sonarr.toncom159.com` | default_server 443 + client cert | Challenge/approve, then proxy | id 4 | `192.168.5.184:8989` |
| `radarr.toncom159.com` | default_server 443 + client cert | Challenge/approve, then proxy | id 3 | `192.168.5.184:7878` |
| `qbittorrent.toncom159.com` | default_server 443 + client cert | Challenge/approve, then proxy | id 1 | `192.168.5.184:8080` (gluetun) |
| `form.riskshields.com` | Dedicated LE 443 | Proxy only (not registered) | id 14 | `192.168.5.184:3001` |
| `drive.riskshields.com` | Dedicated LE 443 | Proxy only (not registered) | id ? | (not tested) |
| any other host | default_server 443 + client cert | Proxy to NPM (no challenge) | NPM decides by Host | NPM default or configured host |

### 10.2 NPM proxy-host summary

```text
id 1  qbittorrent.toncom159.com  → 192.168.5.184:8080
id 3  radarr.toncom159.com       → 192.168.5.184:7878
id 4  sonarr.toncom159.com       → 192.168.5.184:8989
id 14 form.riskshields.com       → 192.168.5.184:3001
```

---

## 11. Runtime Evidence

### 11.1 qBittorrent asset failure (browser)

Selenium/Chromium console output while loading `https://qbittorrent.toncom159.com/` with valid gateway and qBittorrent session cookies:

```text
SEVERE security ... Refused to apply style from '.../css/Window.css?v=9mymzr' because its MIME type ('') is not a supported stylesheet MIME type
SEVERE network ... scripts/cache.js?v=9mymzr - Failed to load resource: the server responded with a status of 500 ()
SEVERE network ... images/torrent-start.svg - Failed to load resource: the server responded with a status of 500 ()
SEVERE javascript ... scripts/lib/mocha.min.js - Uncaught ReferenceError: Hash is not defined
```

### 11.2 Sonarr/Radarr API failure (curl)

Sonarr translation endpoint under concurrency:

```text
50 parallel GET https://sonarr.toncom159.com/api/v3/localization?r=<unique>
→ 19 × HTTP/2 401 (upstream auth rejection)
→ 31 × HTTP/2 500 (gateway crash, empty body)
```

Radarr translation endpoint under concurrency:

```text
20 parallel GET https://radarr.toncom159.com/api/v3/localization/language
→ 12 × HTTP/2 401
→  8 × HTTP/2 500
```

### 11.3 form.riskshields.com under concurrency

```text
20 parallel GET https://form.riskshields.com/
→  7 × HTTP/1.1 200 OK
→ 13 × HTTP/1.1 500 Internal Server Error
```

### 11.4 Backend exception log

`docker logs security-gateway-backend`:

```text
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "...", Request id "...": An unhandled exception was thrown by the application.
      System.InvalidOperationException: A second operation was started on this context instance before a previous operation completed.
         at Microsoft.EntityFrameworkCore.Infrastructure.Internal.ConcurrencyDetector.EnterCriticalSection()
         at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()
         at SecurityGateway.Infrastructure.Applications.Services.ApplicationPolicyService.GetApplicationByDomainAsync(String domain, CancellationToken cancellationToken)
         at SecurityGateway.Api.Middleware.GatewayMiddleware.InvokeAsync(HttpContext context)
```

and

```text
warn: SecurityGateway.Api.Middleware.GatewayMiddleware[0]
      Non-critical IP intelligence tracking failed for 47.145.35.238.
      System.InvalidOperationException: A second operation was started on this context instance before a previous operation completed.
         ...
         at SecurityGateway.Infrastructure.IpIntelligence.IpIntelligenceService.TrackAsync(TrackIpRequest request, CancellationToken cancellationToken)
         at SecurityGateway.Api.Middleware.GatewayMiddleware.InvokeAsync(HttpContext context)
```

---

## 12. Root Causes

### 12.1 CONFIRMED ROOT CAUSE

**Scoped EF Core services are captured by singleton middleware constructors.**

`GatewayMiddleware` is registered with `app.UseMiddleware<GatewayMiddleware>()`. By default ASP.NET Core creates one middleware instance for the application lifetime. Constructor parameters are resolved from the **root service provider**, so scoped services injected into the constructor become de-facto singletons.

The constructor currently takes:

```csharp
public GatewayMiddleware(
    RequestDelegate next,
    IProxyService proxyService,
    IClientIpResolver clientIpResolver,
    IIpIntelligenceService? ipIntelligenceService,
    IApplicationPolicyService applicationPolicyService,
    IAccessControlService accessControlService,
    IAccessRequestService accessRequestService,
    IRateLimitService rateLimitService,
    IAutomaticBlockingService automaticBlockingService,
    IAuditService auditService,
    GatewayOptions options,
    ILogger<GatewayMiddleware> logger)
```

`Program.cs` registers the following as **scoped** (all use `IUnitOfWork` / `ApplicationDbContext`):

* `IApplicationPolicyService`
* `IAccessControlService`
* `IAccessRequestService`
* `IRateLimitService`
* `IAutomaticBlockingService`
* `IAuditService`
* `IIpIntelligenceService`

Because the same middleware instance handles many requests concurrently, the same `DbContext` instance is used by multiple threads simultaneously, producing:

```text
InvalidOperationException: A second operation was started on this context instance before a previous operation completed.
```

`InlineWafMiddleware` has the same anti-pattern: it injects `IWafEventService` (scoped) in its constructor.

### 12.2 POSSIBLE CONTRIBUTING CAUSE

* **High parallel asset load from modern SPAs.** The bug only becomes visible when the browser fires many concurrent requests. Single-request or low-traffic paths often succeed, which is why the issue appeared intermittent.
* **Cloudflare edge caching can mask or amplify perceived flakiness.** A 500 response may be cached briefly, but the underlying cause remains the gateway backend crash.

### 12.3 NOT RESPONSIBLE

| Candidate | Evidence |
|---|---|
| CSP | Permissive deployed policy; no browser CSP violations; failed responses are HTTP 500, not blocked-by-policy |
| nginx proxy configuration | Paths, Host headers, and upstream routing are correct; NPM logs confirm requests reach upstream when the gateway does not crash |
| URL rewriting | No rewrites are applied to application paths; original path is preserved through backend and NPM |
| Host / X-Forwarded-* headers | NPM logs show correct `Host` and client IP is propagated |
| Origin / Referer | Passed through unchanged |
| Cookies | Domain-scoped, Secure, HttpOnly; upstream cookies are forwarded |
| SameSite | `SameSite=None` in deployed image does not cause 500s; repo diff changes it to `Strict` but is not deployed |
| Authentication | Challenge/approval works; login succeeds; failures occur on already-approved sessions |
| `/api` routing | Correctly hostname-dependent; admin API and app APIs are not confused |
| WebSocket | Not used by the failing paths |
| Static asset paths | Original paths reach upstream unchanged |
| Compression / content types | Correct when the gateway returns 200 |
| Redirects / base URL | No incorrect redirects observed |
| Cloudflare | Cloudflare is the transport; the 500 response originates from the gateway backend (see stack trace) |
| NPM | NPM returns 200 when reached; it is not the source of the 500 |
| Application configuration | Sonarr/Radarr/qBittorrent configs are standard; direct LAN access works |
| Firewall / networking | Ports are reachable; failures are application-layer 500s |

---

## 13. Recommended Fixes

**Do not implement these changes as part of this diagnostic.** They are listed for the follow-up repair phase.

### 13.1 Fix middleware DI lifetimes

**Problem:** Singleton middleware holds scoped EF Core services.  
**Root cause:** Constructor injection of scoped services into middleware.  
**Proposed change:**

1. In `GatewayMiddleware`, remove all scoped service constructor parameters except singleton-safe options/logger/proxy/client-IP resolver.
2. Inject `IServiceScopeFactory`.
3. At the start of `InvokeAsync`, create an `AsyncServiceScope`, resolve the scoped services from that scope, and dispose the scope at the end of the request.

Example pattern:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    await using var scope = _serviceScopeFactory.CreateAsyncScope();
    var applicationPolicyService = scope.ServiceProvider.GetRequiredService<IApplicationPolicyService>();
    var accessControlService = scope.ServiceProvider.GetRequiredService<IAccessControlService>();
    // ... etc
}
```

4. Apply the same fix to `InlineWafMiddleware` for `IWafEventService`.

**Security impact:** No weakening. Preserves per-request isolation and audit/rate-limit/access-control integrity.

**Applications affected:** All proxied domains, including Sonarr, Radarr, qBittorrent, and form.riskshields.com.

**How to test:**

* Rebuild the backend Docker image.
* Run 50+ parallel `curl` requests against `https://sonarr.toncom159.com/api/v3/localization`, `https://radarr.toncom159.com/api/v3/localization/language`, `https://qbittorrent.toncom159.com/scripts/cache.js`, and `https://form.riskshields.com/`.
* Confirm zero HTTP 500 responses and that backend logs contain no `DbContext` concurrency exceptions.
* Load each app in a browser and verify:
  * Sonarr / Radarr no longer display `Failed to load translations from API`.
  * qBittorrent main UI renders styled and fully functional.
  * form.riskshields.com remains accessible.

**Rollback:** Re-deploy the previous backend image (`ghcr.io/kamil0103/security-gateway-backend:latest` before the new build).

### 13.2 Optional: isolate IP-intelligence tracking

**Problem:** Even with per-request scopes, `IpIntelligenceService.TrackAsync` performs synchronous-feeling DB writes on every request.  
**Proposed change:** Make tracking fire-and-forget with its own scope (or move to a background channel/worker) so a slow or failing tracking write cannot block or fail application requests. The current code already catches and logs the exception; making it fully asynchronous and out-of-band would remove the small per-request latency and avoid any residual scope-sharing risk.

**Security impact:** No weakening; IP intelligence remains recorded.

**How to test:** Verify `AccessRequests` / `IpAddresses` table continues to record client IPs after the change.

---

## 14. Anti-Hallucination Summary

| Claim | Status | Evidence |
|---|---|---|
| CSP blocking resources | **FALSE** | Deployed CSP is permissive; failures are HTTP 500 with empty body |
| `/api` mis-routed to admin API | **FALSE** | `IsAdminPath` checks hostname; NPM logs show app `/api/*` reaching upstream |
| Host / X-Forwarded headers wrong | **FALSE** | NPM logs show correct `Host` and upstream port |
| Cookies broken | **FALSE** | Cookies scoped per-host and forwarded; login cookies work |
| qBittorrent unstyled due to CSS path rewrite | **FALSE** | Paths are unchanged; CSS returns 200 or 500 depending on concurrency |
| Sonarr/Radarr translation error is upstream bug | **FALSE** | Upstream returns 200 when reached; 500 comes from gateway stack trace |
| form.riskshields.com is an internal toncom159 app | **FALSE** | Not in `Applications` table; has own LE cert and nginx server block |
| Gateway middleware shares DbContext across requests | **TRUE** | Constructor injects scoped services; backend logs show concurrency exceptions |
| Concurrent load reproduces 500s | **TRUE** | Reproduced with curl for Sonarr, Radarr, qBittorrent, and form.riskshields.com |

---

**End of diagnostic.**
