# ADR-010: Abstract Email Provider with SMTP Implementation

## Status

Accepted

## Context

The gateway must send email for password reset and email verification. The notification provider should be abstracted so additional providers (Telegram, Discord, ntfy, web push) can be added later.

## Decision

Introduce `IEmailService` as the email abstraction. The initial implementation uses `System.Net.Mail.SmtpClient` with configurable SMTP settings.

If SMTP is not configured, email verification is automatically skipped during registration (user is marked verified), and password reset tokens are generated but not sent. This keeps the development environment usable without requiring a real SMTP server.

## Alternatives

- **MailKit:** More robust and recommended, but added a vulnerable transitive dependency during implementation. Reverted to `System.Net.Mail` to avoid the vulnerability while keeping functionality.
- **SendGrid/Amazon SES:** External dependencies not suitable for self-hosting.

## Consequences

- SMTP configuration is optional for development.
- Email is the first notification provider; future providers will follow a similar abstraction pattern.
- Production deployments should configure SMTP to enable password reset and verification emails.
