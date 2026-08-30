# ADR-001: Use PostgreSQL as the Primary Database

## Status

Accepted

## Context

The Security Gateway needs a persistent, relational store for users, sessions, devices, IP addresses, security events, audit logs, and application policies.

## Decision

Use PostgreSQL 16 as the primary database.

## Alternatives

- **SQLite:** Insufficient for concurrent production workloads and Unraid deployment.
- **MySQL/MariaDB:** Viable, but PostgreSQL has stronger JSON support, better concurrency semantics, and is commonly used in the .NET ecosystem.
- **SQL Server:** Not ideal for Linux/Unraid self-hosting due to licensing and resource overhead.

## Consequences

- PostgreSQL is the source of truth for persistent data.
- Entity Framework Core with Npgsql provider is used for data access.
- Migrations are managed with EF Core code-first migrations.
- PostgreSQL must not be exposed publicly.
