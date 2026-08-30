# Security Gateway

A self-hosted security gateway designed for an Unraid server running multiple Docker applications. It sits in front of an existing [Nginx Proxy Manager](https://nginxproxymanager.com/) installation and controls whether an incoming request is allowed to reach it.

## Architecture

```
Internet
    ↓
Security Gateway
    ↓
Nginx Proxy Manager
    ↓
Docker Applications
```

Nginx Proxy Manager remains responsible for:

- SSL/TLS certificates
- HTTPS termination
- Reverse proxy configuration
- Domain routing
- Forwarding traffic to Docker applications

The Security Gateway is responsible for:

- Authentication
- Authorization
- IP identification
- Device identification
- Session management
- IP/device trust
- Rate limiting
- Threat detection
- WAF integration
- GeoIP intelligence
- Attack detection
- Automatic blocking
- Security logging
- Notifications
- Analytics
- Dashboard
- Global IP/attack map

## Fail-Closed Requirement

If the Security Gateway is unavailable, external applications must become inaccessible. The gateway is the enforcement point; Nginx Proxy Manager must not be directly reachable from the public Internet.

A secure local Unraid administration path is maintained so the server administrator can recover the system if the gateway fails.

## Technology Stack

- **Frontend:** React 19, TypeScript, Vite
- **Backend:** ASP.NET Core 9, EF Core 9, SignalR
- **Database:** PostgreSQL 16
- **Cache:** Redis 7
- **WAF:** ModSecurity + OWASP Core Rule Set
- **Infrastructure:** Docker, Docker Compose, Unraid

## Quick Start

```bash
# 1. Clone and enter directory
cd security-gateway

# 2. Copy environment file and edit with your secrets
cp .env.example .env

# 3. Start development environment
docker compose up -d

# 4. Access services
# Frontend: http://localhost:3100
# Backend API: http://localhost:5100/api/health
```

See [START_HERE.md](./docs/START_HERE.md) for full setup instructions.

## Documentation

- [Project Specification](./docs/PROJECT_SPECIFICATION.md)
- [Roadmap](./docs/ROADMAP.md)
- [Contributing](./docs/CONTRIBUTING.md)
- [Security](./docs/SECURITY.md)
- [Architecture Decisions](./docs/architecture/decisions/)

## License

MIT
