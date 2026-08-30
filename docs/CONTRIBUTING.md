# Security Gateway — Contributing Guide

## Branch Workflow

Use the following workflow:

```
main
  ↓
feature/short-description
  ↓
Pull Request
  ↓
CI passes
  ↓
Code review
  ↓
Merge
```

Never commit directly to `main` for feature work.

## Branch Naming

- `feature/authentication`
- `feature/device-enrollment`
- `feature/rate-limiting`
- `fix/health-check-timeout`
- `docs/update-roadmap`
- `security/hash-algorithm-update`

## Commit Convention

Use conventional commit messages:

- `feat(module): description`
- `fix(module): description`
- `security(module): description`
- `refactor(module): description`
- `test(module): description`
- `docs: description`
- `chore: description`

Example:

```
feat(auth): add password reset endpoint
```

## Code Style

### Backend

- Follow Clean Architecture: Domain → Application → Infrastructure → Api.
- No business logic in controllers.
- Use `DateTimeOffset` for timestamps.
- Use `Guid` for entity identifiers.
- Use async/await consistently.

### Frontend

- TypeScript for all new code.
- Feature-based folders under `src/modules/`.
- Shared UI components under `src/components/ui/`.
- API client under `src/lib/api.ts`.

## Testing Gate

Every feature must include:

- Unit tests for application services.
- Integration tests for API endpoints where applicable.
- Frontend component tests where applicable.

## Security

- Never commit secrets.
- Never log passwords or tokens.
- Never trust client-provided IP headers without validating the trusted proxy chain.
- Document security assumptions and limitations.

## Pull Request Template

A PR should include:

- What changed and why
- Security considerations
- Testing performed
- Documentation updated
