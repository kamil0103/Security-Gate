# ADR-009: Use JWT Access Tokens with Database-Stored Refresh Tokens

## Status

Accepted

## Context

The gateway needs stateless authentication for API requests to avoid database lookups on every call, while still supporting session revocation and expiration.

## Decision

Use short-lived JWT access tokens for API authorization and long-lived refresh tokens stored in the database for session management.

- Access tokens are signed JWTs containing user ID, username, email, and role. They expire after 15 minutes by default.
- Refresh tokens are opaque 64-byte random strings. Their SHA-256 hash is stored in the `Sessions` table with an expiration date and revocation flag.
- Refresh token rotation is implemented: each use revokes the old token and issues a new one.
- Password changes and explicit logout revoke sessions.

## Alternatives

- **Stateful sessions only:** Simple revocation but requires a database lookup on every request.
- **Long-lived JWTs only:** No revocation capability; compromised tokens cannot be invalidated.
- **OAuth/OIDC:** Not needed for V1 local accounts.

## Consequences

- Access tokens are stateless and fast to validate.
- Sessions can be revoked by marking refresh tokens as revoked.
- Refresh tokens must be protected from leakage (e.g., stored securely by clients).
- Architecture supports future passkeys and WebAuthn without changing the session model.
