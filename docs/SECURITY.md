# Security Gateway — Security Policy

This project is security-critical. All contributors must follow secure development practices.

## Reporting Security Issues

If you discover a security vulnerability, please do not open a public issue. Instead, contact the project owner directly.

Email: security@toncom159.com (replace with actual contact once configured)

## Security Principles

1. **Fail-closed:** If the gateway is unavailable, external access is blocked.
2. **Least privilege:** Services run with the minimum required permissions.
3. **Defense in depth:** Multiple independent security controls.
4. **Assume breach:** Log and monitor everything.
5. **No security by obscurity:** Design is documented and reviewable.

## Prohibited Practices

- Storing plaintext passwords
- Committing secrets or credentials
- Trusting arbitrary forwarded headers
- Disabling authentication for convenience
- Exposing PostgreSQL, Redis, or NPM publicly
- Logging passwords, tokens, or secrets

## Required Practices

- Secure password hashing (Argon2id / PBKDF2)
- Secure cookies (HttpOnly, Secure, SameSite)
- Security headers (HSTS, CSP, X-Frame-Options, etc.)
- Input validation and output encoding
- Authorization checks on every request
- CSRF protection where appropriate
- Rate limiting
- Docker network isolation
- Dependency security scanning

## Network Security

The Security Gateway must correctly distinguish:

- Real client IP
- Trusted proxy IP
- Nginx Proxy Manager IP
- Internal Docker IP
- Local network IP

Only trust proxy headers from explicitly configured trusted proxies. Document the trusted proxy chain.

## Fail-Closed Requirement

Nginx Proxy Manager must not be directly reachable from the public Internet. If the Security Gateway fails, external traffic must be blocked. A secure local Unraid administration path must be available for recovery.

## Audit

Security-critical actions are logged with timestamp, user, IP, device, action, target, result, and metadata. Audit logs must not contain secrets.
