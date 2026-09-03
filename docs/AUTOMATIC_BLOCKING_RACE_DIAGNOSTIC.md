# AUTOMATIC BLOCKING RACE DIAGNOSTIC

> **Status:** `VERIFIED`  
> The duplicate-key failure was reproduced on the live deployed system (`192.168.5.184`) and the exact call stack matches the source-code race path documented below.

---

## 1. Executive Summary

A new, separate concurrency bug exists in the automatic blocking flow. When two or more concurrent gateway requests decide to block the same IP, each request performs a non-atomic *check-then-insert* against the `BlocklistEntries` table. Because the existence check and the insert are not protected by a database-level atomic operation or an explicit serializable transaction, multiple requests can observe "no block exists" and then attempt to insert the same `(Type, Value)` row. PostgreSQL enforces the unique index `IX_BlocklistEntries_Type_Value`; the second committer receives a `23505` duplicate-key error that propagates as an unhandled `DbUpdateException` and surfaces as an HTTP `500`.

This is **not** a schema problem. The unique constraint is correct. The bug is that the application does not handle the inherently concurrent "block this IP" operation idempotently.

---

## 2. Exact Exception

Observed in the live backend container logs (`security-gateway-backend`) during a controlled concurrency test:

```text
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
       ---> Npgsql.PostgresException (0x80004005): 23505: duplicate key value violates unique constraint "IX_BlocklistEntries_Type_Value"
          SqlState: 23505
          MessageText: duplicate key value violates unique constraint "IX_BlocklistEntries_Type_Value"
          SchemaName: public
          TableName: BlocklistEntries
          ConstraintName: IX_BlocklistEntries_Type_Value
          File: nbtinsert.c
          Line: 666
          Routine: _bt_check_unique
         at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(...)
         at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(...)
         at Microsoft.EntityFrameworkCore.Storage.RelationalDatabase.SaveChangesAsync(...)
```

Outer stack trace points directly to the automatic blocking code:

```text
   at SecurityGateway.Infrastructure.Blocking.Services.AutomaticBlockingService.BlockAsync(String ipAddress, Nullable`1 durationMinutes, String reason, CancellationToken cancellationToken) in /src/src/SecurityGateway.Infrastructure/Blocking/Services/AutomaticBlockingService.cs:line 102
   at SecurityGateway.Infrastructure.Blocking.Services.AutomaticBlockingService.CheckAndBlockAsync(String ipAddress, Nullable`1 threatScore, CancellationToken cancellationToken) in /src/src/SecurityGateway.Infrastructure/Blocking/Services/AutomaticBlockingService.cs:line 68
   at SecurityGateway.Infrastructure.AccessControl.Services.AccessRequestService.EvaluateAccessAsync(AccessEvaluationContext context, CancellationToken cancellationToken) in /src/src/SecurityGateway.Infrastructure/AccessControl/Services/AccessRequestService.cs:line 81
   at SecurityGateway.Api.Middleware.GatewayMiddleware.InvokeAsync(HttpContext context) in /src/src/SecurityGateway.Api/Middleware/GatewayMiddleware.cs:line 115
   at SecurityGateway.Api.Middleware.InlineWafMiddleware.InvokeAsync(HttpContext context) in /src/src/SecurityGateway.Api/Middleware/InlineWafMiddleware.cs:line 106
   at SecurityGateway.Api.Middleware.SecurityHeadersMiddleware.InvokeAsync(HttpContext context) in /src/src/SecurityGateway.Api/Middleware/SecurityHeadersMiddleware.cs:line 64
```

---

## 3. Call Chain

The complete request path that produces the exception is:

```
HTTP request (Host = configured application domain)
  └─ SecurityHeadersMiddleware
      └─ InlineWafMiddleware
          └─ GatewayMiddleware.InvokeAsync
              ├─ _clientIpResolver.Resolve(...)                → client IP
              ├─ _ipIntelligenceService.TrackAsync(...)        → creates/updates IpAddresses row
              ├─ _applicationPolicyService.GetApplicationByDomainAsync(...)
              └─ _accessRequestService.EvaluateAccessAsync(...)
                   ├─ _accessControlService.IsBlockedAsync(...)     → reads active blocklist
                   └─ _automaticBlockingService.CheckAndBlockAsync(...)
                        ├─ GetThreatScoreAsync(...)                 → reads IpAddresses.ThreatScore
                        ├─ BlockAsync(...)
                        │   ├─ _blocklistRepository.GetByTypeAndValueAsync(Ip, ipAddress)  ← READ
                        │   ├─ if existing → UpdateAsync(...)
                        │   └─ else        → AddAsync(new BlocklistEntry { Type = Ip, Value = ipAddress })  ← WRITE
                        └─ _unitOfWork.SaveChangesAsync()            ← commits insert (line 102)
                             → Npgsql throws 23505 on duplicate (Type, Value)
```

Key source files:

| File | Role |
|------|------|
| `backend/src/SecurityGateway.Api/Middleware/GatewayMiddleware.cs` | Entry point; line 129 calls `EvaluateAccessAsync`. |
| `backend/src/SecurityGateway.Infrastructure/AccessControl/Services/AccessRequestService.cs` | Line 81 calls `CheckAndBlockAsync`. |
| `backend/src/SecurityGateway.Infrastructure/Blocking/Services/AutomaticBlockingService.cs` | Lines 55-68 and 75-102 contain the read-check-write pattern. |
| `backend/src/SecurityGateway.Infrastructure/AccessControl/Repositories/BlocklistRepository.cs` | `GetByTypeAndValueAsync` uses `AsNoTracking().FirstOrDefaultAsync(...)`. |
| `backend/src/SecurityGateway.Infrastructure/Persistence/ApplicationDbContext.cs` | Lines 139-145 define the unique index on `BlocklistEntry`. |

---

## 4. Race Condition

The problematic code in `AutomaticBlockingService.BlockAsync` is:

```csharp
// AutomaticBlockingService.cs, lines 75-100
var existing = await _blocklistRepository.GetByTypeAndValueAsync(
    BlocklistEntryType.Ip, ipAddress, cancellationToken).ConfigureAwait(false);

if (existing is not null)
{
    existing.IsEnabled = true;
    existing.ExpiresAt = durationMinutes.HasValue ? ... : null;
    existing.Reason = reason;
    await _blocklistRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
}
else
{
    var entry = new BlocklistEntry
    {
        Type = BlocklistEntryType.Ip,
        Value = ipAddress,
        Reason = reason,
        ExpiresAt = ...
    };
    await _blocklistRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
}

await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);   // line 102
```

And the earlier pre-check in `CheckAndBlockAsync`:

```csharp
// AutomaticBlockingService.cs, lines 55-68
var existing = await _blocklistRepository.GetByTypeAndValueAsync(
    BlocklistEntryType.Ip, ipAddress, cancellationToken).ConfigureAwait(false);

if (existing is not null && existing.IsEnabled && (existing.ExpiresAt == null || existing.ExpiresAt > DateTimeOffset.UtcNow))
    return new BlockResultDto { Blocked = true, ... };

return await BlockAsync(ipAddress, durationMinutes, reason, cancellationToken).ConfigureAwait(false);
```

Both checks are plain `SELECT` queries executed outside of any explicit transaction that spans the subsequent `INSERT`. Each HTTP request uses its own scoped `ApplicationDbContext`, so two requests can run the same sequence concurrently.

### Exact interleaving

| Time | Request A | Request B |
|------|-----------|-----------|
| T1 | `SELECT ... WHERE Type=Ip AND Value=X` → no row | |
| T2 | | `SELECT ... WHERE Type=Ip AND Value=X` → no row |
| T3 | `INSERT BlocklistEntry(Type=Ip, Value=X)` | |
| T4 | | `INSERT BlocklistEntry(Type=Ip, Value=X)` |
| T5 | `SaveChanges()` → **success** | |
| T6 | | `SaveChanges()` → **23505 duplicate key** → `DbUpdateException` → HTTP 500 |

`CheckAndBlockAsync` does not prevent this because its own `SELECT` (T1/T2) is also outside a transaction; the duplicate check in `BlockAsync` (lines 75-76) is the one that actually executes immediately before the insert and has the same window.

### Scope of the race

The `AutomaticBlockingService` only creates `BlocklistEntryType.Ip` blocks. Therefore, **the automatic-blocking race is limited to IP addresses**. The same `(Type, Value)` uniqueness logic and read-check-write pattern also exists elsewhere in the codebase, but those paths are not part of the automatic blocking flow:

* `RateLimitService.EscalateAsync` — only creates `BlocklistEntryType.Ip` blocks and uses the same `GetByTypeAndValueAsync` → `AddAsync` pattern.
* `AccessControlService.CreateBlocklistEntryAsync` — manual admin endpoint; checks existence and throws `InvalidOperationException`, but a concurrent admin call could still hit the unique constraint.
* `IpIntelligenceService.TrackAsync` — checks `IpAddresses.Ip` uniqueness before inserting; the `IpAddresses.Ip` column also has a unique index, so the same class of race exists for new IP rows.

---

## 5. Database Constraint

### Table and index

```sql
-- From migration 20260830204347_AddAccessControl.cs
CREATE TABLE "BlocklistEntries" (
    "Id" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Value" character varying(128) NOT NULL,
    "Reason" character varying(500),
    "ExpiresAt" timestamp with time zone,
    "IsEnabled" boolean NOT NULL,
    "CreatedByUserId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_BlocklistEntries" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_BlocklistEntries_Type_Value"
    ON "BlocklistEntries" ("Type", "Value");
```

Live verification on `security-gateway-postgres`:

```text
           indexname            |                                                    indexdef
--------------------------------+-----------------------------------------------------------------------------------------------------------------
 PK_BlocklistEntries            | CREATE UNIQUE INDEX "PK_BlocklistEntries" ON public."BlocklistEntries" USING btree ("Id")
 IX_BlocklistEntries_Type_Value | CREATE UNIQUE INDEX "IX_BlocklistEntries_Type_Value" ON public."BlocklistEntries" USING btree ("Type", "Value")
```

### Assessment

* **The constraint is correct.** Preventing duplicate `(Type, Value)` pairs is desirable security behavior.
* **No collation override is configured**, so the unique comparison uses the database default collation and is case-sensitive. For IP values this is fine because the client-IP resolver normalizes IPv4-mapped IPv6 addresses (`MapToIPv4`) before the value reaches the blocklist.
* **No normalization is applied inside the insert code.** The value stored is exactly the string passed in. For IPs this is currently consistent because normalization happens upstream in the resolver.
* **The problem is not the constraint; it is the application's failure to perform an atomic idempotent insert/upsert in the presence of the constraint.**

---

## 6. Runtime Reproduction

### Methodology

All traffic was generated from the Unraid host that runs the deployment, targeting the backend container directly over the Docker bridge so that `X-Forwarded-For` could be controlled without affecting external services.

1. **Pick a fresh TEST-NET-3 IP** (e.g. `203.0.113.51`) that is not already blocked.
2. **Create an `IpAddresses` row** by sending one benign gateway request with that IP in `X-Forwarded-For`.
3. **Raise the IP threat score to 45** (above the default `MediumThreshold` of 40) by POSTing three high-severity WAF events to `/api/waf-events` using the admin bypass host (`admin.toncom159.com`) so that `GatewayMiddleware` does not challenge the request.
4. **Fire 100 concurrent gateway requests** to a configured application domain (`event.toncom159.com`) with the same `X-Forwarded-For` IP.
5. **Record HTTP status codes** and backend logs.

Commands used (example for `203.0.113.51`):

```bash
TEST_IP=203.0.113.51

# 1. Create IpAddresses row
curl -s -o /dev/null --resolve event.toncom159.com:8080:172.20.0.7 \
  "http://event.toncom159.com:8080/?init=1" -H "X-Forwarded-For: $TEST_IP"

# 2. Inject three High-severity WAF events
for i in 1 2 3; do
  curl -s -o /dev/null --resolve admin.toncom159.com:8080:172.20.0.7 \
    http://admin.toncom159.com:8080/api/waf-events \
    -H "Content-Type: application/json" -H "X-Forwarded-For: $TEST_IP" \
    -d "{\"timestamp\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"sourceIp\":\"$TEST_IP\",\"requestId\":\"race-$i\",\"ruleId\":\"942100\",\"ruleMessage\":\"SQL injection\",\"severity\":\"High\",\"attackType\":\"SqlInjection\",\"method\":\"GET\",\"uri\":\"/?id=1 UNION SELECT\",\"host\":\"event.toncom159.com\",\"action\":\"Blocked\",\"rawLog\":\"GET /?id=1 UNION SELECT\"}"
done

# 3. Concurrent burst
CONCURRENCY=100
for i in $(seq 1 $CONCURRENCY); do
  ( curl -s -o /dev/null -w "%{http_code}\n" \
      --resolve event.toncom159.com:8080:172.20.0.7 \
      "http://event.toncom159.com:8080/?race=$i" \
      -H "X-Forwarded-For: $TEST_IP" --max-time 10 ) &
done
wait
```

### Results

Run on `203.0.113.51` (100 concurrent requests):

```text
--- status counts ---
     92 403
      8 500
```

Backend logs for that run showed **8** `DbUpdateException` entries with PostgreSQL error `23505` on `IX_BlocklistEntries_Type_Value`, all originating from `AutomaticBlockingService.BlockAsync` line 102.

A repeat run on a fresh IP (`203.0.113.52`) with the same 100-request concurrency produced:

```text
--- status counts ---
    100 403
```

A third run on `203.0.113.53` with 150 concurrent requests also produced:

```text
--- status counts ---
    150 403
```

### Interpretation

* The race is **intermittent**. It depends on whether two or more requests both pass the existence check before the first `SaveChanges` commits.
* When the race occurs, the first request succeeds and creates the block; the losers fail with HTTP 500.
* No bypass occurred: the blocklist always ended with exactly **one** active entry for the test IP.

After the first reproduction the database contained exactly one row:

```text
                  Id                  | Type |    Value     |                Reason                |           ExpiresAt           | IsEnabled |
--------------------------------------+------+--------------+--------------------------------------+-------------------------------+-----------+
 a7829988-372c-4c76-9e1a-5a87e7423476 |    0 | 203.0.113.51 | Automatic block: medium threat score | 2026-09-03 00:21:57.166074+00 |     t     |
```

---

## 7. Security Impact

| Question | Answer |
|----------|--------|
| Does the first request successfully create the block? | **Yes.** The unique constraint guarantees exactly one row is committed. |
| Does the second request fail only after the block exists? | **Yes.** The failure is the PostgreSQL unique-constraint rejection of the duplicate `(Type, Value)`. |
| Does the exception roll back unrelated security state? | **No.** Each request has its own scoped `DbContext`. `IpIntelligenceService.TrackAsync` already called `SaveChanges` before `CheckAndBlockAsync`, so IP tracking persists. The failed request simply never commits its own duplicate `BlocklistEntries` row. |
| Does automatic blocking still succeed overall? | **Yes**, for the request that commits first. The blocked IP is still blocked on retry. |
| Could an attacker exploit this to avoid being blocked? | **Not reliably.** The block is created; a concurrent request that receives `500` can be retried and will then be blocked with `403`. |
| Does the exception produce HTTP 500? | **Yes**, in the gateway path. The `DbUpdateException` is unhandled and propagates to Kestrel. |
| Does it create excessive audit events? | **No audit is logged for the failing requests** because `AuditService.LogAsync` runs after `SaveChanges` in `BlockAsync`. However, once the block exists, every subsequent request that hits `AccessControlService.IsBlockedAsync` records a `SecurityEvent` of type `AccessBlocked`, which can generate many events under a sustained attack. |
| Does it create notification spam? | **No.** Notifications are dispatched from the access-request challenge flow, not from the automatic blocking path. |
| Does it cause transaction rollback of threat scoring? | **No.** Threat scoring and IP tracking are saved in separate `SaveChanges` calls before the blocklist insert is attempted. |
| Does it leave the system in an inconsistent state? | **No schema/data inconsistency.** The database remains consistent because the unique constraint rejects the duplicate. The operational inconsistency is that some requests return `500` instead of the intended `403`. |

### Real-world effect

During a real attack, the operator will see:

* Sporadic HTTP `500` responses mixed with `403` responses from the same attacker IP.
* `DbUpdateException` noise in the application logs.
* A working blocklist entry (the block does take effect).

The most serious consequence is **availability/reliability degradation**: malicious traffic is correctly blocked, but the gateway also emits `500` errors and exception stack traces, which can mask other issues and pollute monitoring.

---

## 8. Root Cause Classification

### CONFIRMED ROOT CAUSE

* **Non-atomic check-then-insert in `AutomaticBlockingService.BlockAsync`.** The code reads `BlocklistEntries` to decide whether to insert, then inserts and saves, with no database-level atomicity between the read and the write. Under concurrency, multiple requests observe the same empty state and attempt the same insert.

### CONTRIBUTING FACTORS

* **Scoped `DbContext` per request.** Each request has its own unit of work, so there is no application-level synchronization.
* **Default read-committed isolation.** EF Core/Npgsql uses `ReadCommitted` by default; the `SELECT` in `GetByTypeAndValueAsync` does not lock the future unique key.
* **No explicit transaction spanning the existence check and the insert.** `SaveChanges` creates an implicit transaction only around the write batch.
* **No duplicate-key handling.** The code does not catch `PostgresException`/`DbUpdateException` for `SqlState = 23505` and treat it as "already blocked."
* **No idempotency strategy.** Repeated "block this IP" operations are expected to converge to a single row, but the implementation does not use `INSERT ... ON CONFLICT`, `MERGE`, serializable transactions, or distributed locking.

### POSSIBLE CAUSES (CONSIDERED AND RULED OUT AS PRIMARY)

* Multiple backend instances — the current deployment runs only one `security-gateway-backend` container. The race is reproducible with a single instance, so multi-instance contention is not the root cause (though it would make the race more frequent).
* Middleware double execution — `GatewayMiddleware`, `InlineWafMiddleware`, and `SecurityHeadersMiddleware` are each registered once; the stack trace shows a single execution.
* Retry behavior — no Polly/EF retry strategy is configured; nothing is retrying the insert.
* Transaction isolation bug — the default isolation is normal; the issue is the absence of a transaction/atomic operation, not an incorrect one.

### NOT RESPONSIBLE

* The previous middleware DI lifetime repair — `IAutomaticBlockingService` is correctly scoped (`AddScoped`) and `DbContext` is scoped. The duplicate-key exception originates at `SaveChanges` inside `BlockAsync`, not from a shared context.
* The PostgreSQL unique index — the index is correct and is the mechanism that *exposes* the race, not the cause of it.
* Threat scoring logic — scoring correctly raised the IP threat score; the failure occurs only when multiple requests act on that score simultaneously.

---

## 9. Recommended Fix

### Problem

`AutomaticBlockingService.BlockAsync` is not idempotent. Concurrent "block IP X" operations can perform the same insert, causing `DbUpdateException` and HTTP 500.

### Root cause

Read-check-write pattern without an atomic insert/upsert.

### Proposed change

Replace the check-then-insert logic with a **database-native idempotent upsert** using PostgreSQL `INSERT ... ON CONFLICT ("Type", "Value") DO UPDATE ...`. This makes the operation atomic and idempotent: the first committer inserts, the others update the existing row (refreshing `IsEnabled`, `ExpiresAt`, and `Reason`), and no exception occurs.

Because the project targets `net9.0` with EF Core 9, EF Core does not yet expose a cross-database upsert API. The safest approach within the existing architecture is to add an `UpsertAsync` method to `IBlocklistRepository` / `BlocklistRepository` that executes parameterized raw SQL via `context.Database.ExecuteSqlInterpolatedAsync` or `ExecuteSqlRawAsync`, returning the resulting row ID. `AutomaticBlockingService.BlockAsync` then calls `UpsertAsync` instead of `GetByTypeAndValueAsync` + `AddAsync`/`UpdateAsync`.

Example shape (not implemented):

```sql
INSERT INTO "BlocklistEntries" ("Id", "Type", "Value", "Reason", "ExpiresAt", "IsEnabled", "CreatedAt")
VALUES (@id, @type, @value, @reason, @expiresAt, true, @now)
ON CONFLICT ("Type", "Value")
DO UPDATE SET
    "IsEnabled" = true,
    "ExpiresAt" = EXCLUDED."ExpiresAt",
    "Reason"    = EXCLUDED."Reason";
```

`BlockAsync` can then log the audit unconditionally after the upsert succeeds.

### Files likely affected

* `backend/src/SecurityGateway.Application/AccessControl/IBlocklistRepository.cs` — add `UpsertAsync` (or similar).
* `backend/src/SecurityGateway.Infrastructure/AccessControl/Repositories/BlocklistRepository.cs` — implement the PostgreSQL upsert.
* `backend/src/SecurityGateway.Infrastructure/Blocking/Services/AutomaticBlockingService.cs` — replace check-then-insert with upsert.

Optionally apply the same pattern to:

* `backend/src/SecurityGateway.Infrastructure/RateLimiting/Services/RateLimitService.cs` (`EscalateAsync`).
* `backend/src/SecurityGateway.Infrastructure/AccessControl/Services/AccessControlService.cs` (`CreateBlocklistEntryAsync`) if manual concurrent creation is a concern.

### Database impact

* **No schema change.** The existing unique index is used as the conflict target.
* **No index removal.** The constraint remains the authoritative guard against duplicates.
* **One new operation** uses the index for conflict resolution instead of failing.

### Security impact

* No change to blocking semantics: the same IP is still blocked.
* Eliminates HTTP 500 responses from the automatic blocking path.
* Removes exception noise from logs.
* Prevents any theoretical edge case where a swallowed exception in the WAF path (which currently catches `DbUpdateException` from threat detection) could delay block creation.

### Testing strategy

1. **Integration test** against a real PostgreSQL container (in-memory EF cannot exercise `ON CONFLICT`).
2. Fire concurrent requests from multiple threads/processes against the same IP and assert:
   * All responses are `403` (or the expected challenge/block response), never `500`.
   * Exactly one `BlocklistEntries` row exists for the IP.
   * No `DbUpdateException` appears in logs.
3. **Unit tests** for the repository method can mock `ExecuteSqlInterpolatedAsync` or test via the integration factory.

### Rollback strategy

* Revert the upsert call back to the previous `GetByTypeAndValueAsync` + `AddAsync`/`UpdateAsync` implementation.
* No database migration is required for rollback.

---

## 10. Remaining Risks

1. **Same race in `IpIntelligenceService.TrackAsync`.** A new IP can be inserted by two concurrent first requests because `GetByIpAsync` is followed by `AddAsync`. The `IpAddresses.Ip` unique index would reject the duplicate with a similar `DbUpdateException`. This was not reproduced during this diagnostic but has the same structural cause.
2. **Same race in `RateLimitService.EscalateAsync`.** Rate-limit escalation uses the same `GetByTypeAndValueAsync` → `AddAsync` pattern for IP blocks.
3. **Same race in `AccessControlService.CreateBlocklistEntryAsync`.** Concurrent admin/API creation of the same `(Type, Value)` can hit the unique constraint, although this path throws `InvalidOperationException` before the insert in the non-concurrent case.
4. **WAF path silently absorbs the exception.** `WafEventService.RecordSecurityEventAsync` catches all exceptions from `_threatDetectionService.RecordEventAsync`, which includes the automatic blocking call. A duplicate-key race on that path will not produce HTTP 500 but may mean the block is created by whichever request commits first; the swallowing hides the symptom but does not fix the race.
5. **No retry policy for transient failures.** The application does not use `EnableRetryOnFailure` or Polly, so any transient Npgsql error (including serialization failures if a serializable-transaction approach were chosen) would propagate directly.
6. **Single CPU backend container.** The current container is limited to 1.0 CPU; while this lowers the race probability, it does not eliminate it, as verified.

---

## Appendix: Evidence Summary

| Evidence | Source |
|----------|--------|
| Source-code read-check-write path | `AutomaticBlockingService.cs:55-68`, `75-102` |
| Unique index definition | `ApplicationDbContext.cs:139-145` and migration `20260830204347_AddAccessControl.cs:70-74` |
| Live index definition | `pg_indexes` query against `security-gateway-postgres` |
| Exception with `23505` | `docker logs security-gateway-backend` during reproduction |
| Call stack to `BlockAsync:102` | Same backend logs |
| HTTP status split (92×403, 8×500) | Controlled 100-request concurrency test |
| Single-instance deployment | `docker ps` on `192.168.5.184` showed one `security-gateway-backend` container |

---

*Document generated during a read-only diagnostic. No code, schema, or security-behavior changes were made on the target system.*
