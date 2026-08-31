# ADR-025 — Cloudflare Integration

## Status

Accepted

## Context

Many self-hosted deployments place Cloudflare in front of the origin server for DDoS protection, CDN caching, and WAF features. When Cloudflare proxies traffic, the TCP connection to the gateway originates from a Cloudflare IP address, and the real client IP is delivered in headers such as `CF-Connecting-IP` or `CF-Visitor-IP`. The gateway must safely restore the real client IP only when the direct peer is actually a Cloudflare IP, and it should be able to enforce policies based on Cloudflare-provided metadata such as the `CF-IPCountry` header.

## Decision

We implemented Cloudflare-aware client IP resolution and policy enforcement.

Key design points:

- **Cloudflare IP ranges**: `ICloudflareIpService` with a default `CloudflareIpService` implementation ships with Cloudflare's published IPv4 ranges. Administrators can override ranges via `SecurityGateway:Cloudflare:IpRanges` or refresh them through the controller in the future.
- **Client IP resolver decorator**: `CloudflareClientIpResolver` wraps the existing `IClientIpResolver`. When Cloudflare integration is enabled and the direct remote IP is a Cloudflare IP, it returns the value from `CF-Connecting-IP` (or `CF-Visitor-IP` when configured). The original Cloudflare IP is appended to the proxy chain.
- **Header capture**: `GatewayMiddleware.BuildClientIpContext` copies Cloudflare headers into `ClientIpContext.AdditionalHeaders` so downstream resolvers can use them.
- **Cloudflare-aware policies**: `ApplicationPolicy` gained `AllowedCloudflareCountries`, `BlockedCloudflareCountries`, and `BypassAuthenticationPaths`. The policy service evaluates country rules using the `CF-IPCountry` header and allows path-based authentication bypass for streaming endpoints.
- **Admin endpoints**: `CloudflareController` exposes status, range refresh, and IP-check endpoints restricted to administrators.
- **Migration**: `AddCloudflarePolicyFields` adds the new policy columns to the database.

## Consequences

- **Pros**:
  - Restores the real client IP for Cloudflare-proxied traffic without blindly trusting arbitrary `X-Forwarded-For` values.
  - Supports Cloudflare-specific policy rules (country allow/block) without requiring GeoIP lookups.
  - Path bypass enables performance-sensitive streaming endpoints to skip authentication while keeping other paths protected.

- **Cons**:
  - Only IPv4 ranges are included by default; IPv6 and dynamic range refresh are not yet implemented.
  - Cloudflare country codes are taken from a request header and assume Cloudflare is the trusted proxy; spoofed headers from non-Cloudflare peers are ignored because the resolver checks the direct IP first.

## Alternatives Considered

- **Trust all `X-Forwarded-For` values from Cloudflare**: Rejected because we still need to verify that the direct peer is a Cloudflare IP before trusting Cloudflare-specific headers.
- **GeoIP-based country policies**: Deferred in favor of `CF-IPCountry` because it is available on every Cloudflare-proxied request and avoids a GeoIP database dependency.
