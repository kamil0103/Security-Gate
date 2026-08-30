# ADR-002: Use Redis for Ephemeral State and Counters

## Status

Accepted

## Context

The gateway must handle high-frequency ephemeral state such as rate-limit counters, temporary bans, temporary challenges, and real-time counters.

## Decision

Use Redis 7 for ephemeral state and high-frequency counters.

## Alternatives

- **PostgreSQL only:** Would create unnecessary write load and complexity for short-lived counters.
- **In-memory caching:** Not shared across multiple backend instances and is lost on restart.
- **Memcached:** Viable, but Redis offers richer data structures and persistence options.

## Consequences

- Redis is used for rate limiting, temporary bans, temporary challenges, session-related ephemeral state, and real-time counters.
- PostgreSQL remains the persistent source of truth.
- Redis must not be exposed publicly.
