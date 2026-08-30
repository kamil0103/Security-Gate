# Security Gateway — Start Here

Welcome to the Security Gateway project. This guide will get the development environment running on your local machine.

## Prerequisites

- Docker Engine 24.0+
- Docker Compose v2+
- Git
- .NET 9 SDK (for local backend development)
- Node.js 20+ (for local frontend development)

## 1. Clone the Repository

```bash
git clone https://github.com/kamil0103/Security-Gate.git
cd Security-Gate
```

## 2. Configure Environment

```bash
cp .env.example .env
```

Edit `.env` and set strong passwords for development. The defaults are fine for local testing but must not be used in production.

## 3. Start the Development Environment

```bash
docker compose up -d
```

This starts:

- PostgreSQL on `localhost:5433`
- Redis on `localhost:6380`
- Backend API on `localhost:5100`
- Frontend on `localhost:3100`
- Nginx Proxy Manager placeholder on `localhost:8091`

## 4. Verify Services

### Backend Health

```bash
curl http://localhost:5100/api/health
```

Expected response:

```json
{
  "status": "Healthy",
  "postgresConnected": true,
  "redisConnected": true,
  "timestamp": "2026-08-30T12:00:00Z"
}
```

### Gateway Proxying

After Docker Compose is running, the gateway proxies non-admin traffic to the NPM placeholder:

```bash
curl http://localhost:5100/some-app -H "X-Forwarded-For: 198.51.100.5"
```

Expected response:

```
Nginx Proxy Manager placeholder - Security Gateway is not yet routing here.
```

This confirms the gateway is receiving requests and forwarding them upstream.

### Authentication

A default admin user is created on first startup:

- Username: `admin`
- Email: `admin@toncom159.com`
- Password: `ChangeMeInProduction123!` (change this in `.env`)

Log in:

```bash
curl -X POST http://localhost:5100/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"usernameOrEmail":"admin","password":"ChangeMeInProduction123!"}'
```

Access your profile:

```bash
curl http://localhost:5100/api/auth/me \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### Frontend

Open [http://localhost:3100](http://localhost:3100) in your browser. You should see the Security Gateway status page.

## 5. Run Tests

### Backend

```bash
cd backend
dotnet test
```

### Frontend

```bash
cd frontend
npm install
npm run test
```

## 6. Local Development Without Docker

### Backend

```bash
cd backend/src/SecurityGateway.Api
dotnet run
```

Requires PostgreSQL on `localhost:5433` and Redis on `localhost:6380` (started via Docker Compose).

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## 7. Stopping the Environment

```bash
docker compose down
```

To remove all data volumes:

```bash
docker compose down -v
```

## Next Steps

Once the development environment is stable, read the [Project Specification](./PROJECT_SPECIFICATION.md) and [Roadmap](./ROADMAP.md) to understand the full vision.
