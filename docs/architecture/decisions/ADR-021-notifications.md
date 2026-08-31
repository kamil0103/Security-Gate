# ADR-021 — Notifications

## Status

Accepted

## Context

Security events such as high-severity attacks, access blocks, and rate limiting need to alert administrators in real time. We needed a notification system that supports multiple channels (email, Telegram, Discord, ntfy, web push) while keeping the provider implementations decoupled from the event generation logic.

## Decision

We built a notification subsystem around channels, providers, a dispatcher, and a service.

Key design points:

- **Domain model**: `NotificationChannel` stores channel type and JSON configuration. `NotificationLog` records every send attempt with status and error details.
- **Provider abstraction**: `INotificationChannelProvider` defines `CanHandle(NotificationChannelType)` and `SendAsync`. Each provider implements one channel type. Providers are registered in DI and resolved via `IEnumerable<INotificationChannelProvider>`.
- **Implemented providers**:
  - `EmailNotificationProvider` reuses the existing `IEmailService` SMTP implementation.
  - `TelegramNotificationProvider` posts to the Telegram Bot API.
  - `DiscordNotificationProvider` posts rich embeds to a Discord webhook.
  - `NtfyNotificationProvider` publishes to an ntfy topic.
  - `WebPushNotificationProvider` validates VAPID configuration but defers actual push delivery to a future iteration.
- **Dispatcher**: `INotificationDispatcher` listens for `SecurityEvent` dispatches and sends notifications through all enabled channels when severity is High or Critical.
- **Integration point**: `ThreatDetectionService.RecordEventAsync` invokes the dispatcher after persisting the event, making notifications automatic for all event creators.
- **Admin API**: `NotificationsController` provides CRUD for channels, test sends, and recent logs.

## Consequences

- **Pros**:
  - Pluggable provider model makes adding new channels straightforward.
  - Centralized dispatch from threat detection keeps notification logic out of individual services.
  - Logs provide an audit trail of sent and failed notifications.

- **Cons**:
  - WebPush is not fully implemented; it only validates configuration.
  - Configuration is stored as JSON; strongly typed per-channel tables would be more robust for complex settings.
  - Synchronous dispatch can slow event recording if providers are slow; background dispatch could be added later.

## Alternatives Considered

- **Outbox pattern with background worker**: Deferred to keep the initial implementation simple.
- **Separate notification microservice**: Rejected as overkill for the current monolithic architecture.
