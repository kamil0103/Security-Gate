# ADR-005: Validate Trusted Proxy Chains for Client IP Resolution

## Status

Accepted

## Context

The Security Gateway must identify the real client IP address for security decisions, logging, and GeoIP lookups. However, the gateway sits behind one or more proxies (Nginx Proxy Manager, Cloudflare in the future, local reverse proxies), so the TCP connection IP is not the real client IP.

Common headers include:

- `X-Forwarded-For`
- `X-Real-IP`
- `Forwarded` (RFC 7239)

These headers can be forged by clients. Trusting them blindly would allow attackers to spoof their IP address.

## Decision

Implement explicit trusted proxy chain validation. Only headers originating from configured trusted proxies are used for client IP resolution.

The algorithm:

1. The direct connection IP (`RemoteIpAddress`) must be in the trusted proxy list.
2. If trusted, parse `X-Forwarded-For` from right to left (rightmost is closest to the server).
3. Each IP in the chain must be a trusted proxy until the first untrusted IP is found; that IP is the client.
4. If all IPs are trusted, the leftmost IP is the client.
5. If the direct connection is not trusted, use it as the client IP and ignore forwarded headers.

Trusted proxies are configured as a comma-separated list of IP addresses or CIDR ranges.

## Alternatives

- **Trust first header blindly:** Simple but insecure; allows IP spoofing.
- **Use only `X-Real-IP`:** Less information; still requires trust validation.
- **Built-in ASP.NET Core ForwardedHeaders middleware:** Provides similar functionality but is less explicit and harder to extend with per-request security decisions.

## Consequences

- Client IP resolution is secure by default.
- Configuration must accurately list all trusted proxies.
- Misconfiguration can result in incorrect client IP attribution.
- The resolver is abstracted behind `IClientIpResolver` for testability and future extension.
