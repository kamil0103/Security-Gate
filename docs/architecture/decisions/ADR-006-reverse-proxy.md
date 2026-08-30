# ADR-006: Implement Gateway as a Reverse Proxy to Nginx Proxy Manager

## Status

Accepted

## Context

The Security Gateway must sit in front of Nginx Proxy Manager (NPM) and control whether each request reaches it. NPM remains responsible for TLS termination, reverse proxy rules, and domain routing.

## Decision

Implement the gateway as a reverse proxy using ASP.NET Core middleware. Non-administrative requests are forwarded to the configured upstream NPM URL. Administrative routes (`/api/*`, `/swagger`) are served directly by the gateway.

The gateway:

- Copies the request method, path, query string, and headers.
- Adds `X-Forwarded-For` and `X-Real-IP` headers based on the validated client IP.
- Streams the response back to the client.
- Returns `502 Bad Gateway` if the upstream is unavailable.

## Alternatives

- **YARP (Yet Another Reverse Proxy):** A robust option, but custom middleware gives tighter control over security decisions and logging.
- **IIS/NGINX as the gateway:** Would require additional infrastructure and reduce the ability to apply application-level security logic.

## Consequences

- Gateway logic is centralized in .NET, enabling deep security integration.
- The proxy service is abstracted behind `IProxyService` for testability.
- Performance characteristics must be monitored; streaming responses avoids buffering large payloads.
