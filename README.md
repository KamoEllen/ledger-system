# Ledger System

![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Angular](https://img.shields.io/badge/Angular-17-DD0031?logo=angular)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)
![Tests](https://img.shields.io/badge/Tests-xUnit-512BD4?logo=.net)

Double-entry ledger-based payment API built with .NET 8, PostgreSQL, and Angular.
Tracks money movement across user wallets using immutable ledger entries, ACID transactions, and idempotent transfer endpoints.

---

## Table of Contents

1. [Tech Stack](#tech-stack)
2. [Milestones](#milestones)
3. [Project Structure](#project-structure)
4. [Prerequisites](#prerequisites)
5. [Configuration — Replace Placeholder Values](#configuration--replace-placeholder-values)
6. [Setup Instructions](#setup-instructions)
7. [Running the Project](#running-the-project)
8. [Running Tests](#running-tests)
9. [API Overview](#api-overview)
10. [Key Engineering Decisions](#key-engineering-decisions)
11. [DevOps](#devops)

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | .NET 8 / ASP.NET Core |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 16 |
| Auth | JWT (access + refresh tokens) |
| Frontend | Angular 17 |
| Testing | xUnit + WebApplicationFactory |
| Containerisation | Docker + Docker Compose |
| Reverse Proxy | Nginx |
| CI/CD | GitHub Actions |

---

## Milestones

Development is structured into 9 sequential milestones. Each milestone is independently deployable and testable before the next begins.

### M1 — Solution Structure + Domain Layer
Set up clean/layered architecture. Define core domain entities with zero framework dependencies.

**Delivers:**
- `LedgerSystem.sln` with four projects: `API`, `Application`, `Domain`, `Infrastructure`
- Domain entities: `User`, `Wallet`, `Transfer`, `LedgerEntry`, `IdempotencyKey`
- Domain exceptions: `InsufficientFundsException`, `WalletFrozenException`, `CurrencyMismatchException`
- Domain value objects: `Money`, `Currency`
- No EF Core, no ASP.NET — pure C# only at this layer

### M2 — Database Schema + Migrations
Wire up Entity Framework Core to PostgreSQL. Define the full schema and create migrations.

**Delivers:**
- EF Core `DbContext` with all entity configurations
- Fluent API constraints: `CHECK (balance >= 0)`, `CHECK (amount > 0)`, unique indexes
- Initial migration covering all tables
- Seed data: default admin user, test wallets with opening balances
- `NUMERIC(19,4)` for all money columns

### M3 — Authentication
User registration and login with JWT. Stateless auth with refresh token rotation.

**Delivers:**
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- BCrypt password hashing
- JWT access tokens (15 min expiry) + refresh tokens (7 day expiry) stored in DB
- Auth middleware wired into pipeline

### M4 — Wallet Management
Full wallet CRUD. Balance is derived from ledger entries and denormalised for performance.

**Delivers:**
- `GET  /api/wallets` — list authenticated user's wallets
- `POST /api/wallets` — create wallet
- `GET  /api/wallets/{id}` — wallet detail + current balance
- `GET  /api/wallets/{id}/history` — paginated ledger history
- Input validation (currency code format, duplicate wallet guard)

### M5 — Transfer Service + Ledger
The critical milestone. Atomic fund transfers with double-entry ledger recording.

**Delivers:**
- `POST /api/transfers` — initiate transfer
- `GET  /api/transfers/{id}` — transfer status
- `GET  /api/transfers` — list user's transfers
- ACID transaction wrapping all balance updates + ledger writes
- Deadlock-safe wallet locking (`SELECT FOR UPDATE` in consistent UUID order)
- `balance_after` snapshot on every ledger entry
- Automatic rollback on any failure

### M6 — Idempotency + Middleware
Make the API safe for retries. Add cross-cutting concerns.

**Delivers:**
- `Idempotency-Key` header enforcement on all mutating endpoints
- Server-side deduplication table with 24-hour TTL
- Duplicate requests return the original response — not re-executed
- Global exception handler middleware (consistent error envelope)
- Request/response logging middleware (Serilog)
- Rate limiting middleware (per-user, per-IP)
- FluentValidation on all request DTOs

### M7 — RBAC + Admin API
Role-based access. Three roles: `user`, `finance`, `admin`.

**Delivers:**
- ASP.NET Core policy-based authorisation
- Role claim embedded in JWT
- `GET  /api/admin/users` — list all users (finance+)
- `GET  /api/admin/users/{id}/wallets` — any user's wallets (finance+)
- `GET  /api/admin/ledger` — full system ledger audit (finance+)
- `POST /api/admin/wallets/{id}/freeze` — freeze wallet (admin only)
- `POST /api/admin/wallets/{id}/unfreeze` — unfreeze wallet (admin only)

### M8 — Testing
Full test suite covering unit, integration, and concurrency scenarios.

**Delivers:**
- **Unit tests** — domain entities, transfer service, idempotency service (mocked I/O)
- **Integration tests** — all endpoints via `WebApplicationFactory` against real test PostgreSQL
- **Concurrency tests** — 10 parallel transfers against a wallet, assert no overdraft occurs
- **Idempotency tests** — duplicate key returns `200` with original body, no double debit
- Test database lifecycle: create → migrate → seed → test → teardown

### M9 — DevOps
Containerise and automate.

**Delivers:**
- `Dockerfile` (multi-stage build — SDK image to runtime image)
- `docker-compose.yml` — API + PostgreSQL + Nginx
- `nginx.conf` — reverse proxy, SSL termination placeholder, request buffering
- `.github/workflows/ci.yml` — build, test, docker build on every push/PR
- `.github/workflows/cd.yml` — deploy on merge to `main` (placeholder targets)
- Health check endpoint: `GET /health`
- Structured JSON logs in production

---

## Project Structure

```
ledger-system/
├── src/
│   ├── LedgerSystem.API/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Program.cs
│   ├── LedgerSystem.Application/
│   │   ├── Services/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   └── Validators/
│   ├── LedgerSystem.Domain/
│   │   ├── Entities/
│   │   ├── Exceptions/
│   │   └── ValueObjects/
│   └── LedgerSystem.Infrastructure/
│       ├── Persistence/
│       │   ├── LedgerDbContext.cs
│       │   ├── Migrations/
│       │   └── Configurations/
│       └── Repositories/
├── tests/
│   ├── LedgerSystem.UnitTests/
│   └── LedgerSystem.IntegrationTests/
├── frontend/
│   └── ledger-angular/          # Angular 17 app (M9+)
├── docker-compose.yml
├── docker-compose.test.yml
├── nginx.conf
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── cd.yml
├── PROJECT_REPORT.md
└── README.md
```

---

## Prerequisites

Ensure the following are installed before setup:

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 8.x | https://dotnet.microsoft.com/download |
| PostgreSQL | 16.x | https://www.postgresql.org/download |
| Docker | 24.x+ | https://docs.docker.com/get-docker |
| Docker Compose | v2.x | Bundled with Docker Desktop |
| Node.js | 20.x LTS | https://nodejs.org (for Angular frontend) |
| Angular CLI | 17.x | `npm install -g @angular/cli` |
| EF Core CLI | 8.x | `dotnet tool install --global dotnet-ef` |

---

## Configuration — Replace Placeholder Values

All placeholder values are prefixed with `REPLACE_` so they are easy to find with a global search.

### Step 1 — Find all placeholders

```bash
grep -r "REPLACE_" . --include="*.json" --include="*.yml" --include="*.env*"
```

### Step 2 — Replace each value

| Placeholder | What to replace it with | Where it appears |
|---|---|---|
| `REPLACE_DB_HOST` | PostgreSQL host (e.g. `localhost` or Docker service name `db`) | `appsettings.json`, `docker-compose.yml` |
| `REPLACE_DB_PORT` | PostgreSQL port (default: `5432`) | `appsettings.json`, `docker-compose.yml` |
| `REPLACE_DB_NAME` | Database name (e.g. `ledger_dev`) | `appsettings.json`, `docker-compose.yml` |
| `REPLACE_DB_USER` | PostgreSQL username | `appsettings.json`, `docker-compose.yml` |
| `REPLACE_DB_PASSWORD` | PostgreSQL password — use a strong random string in production | `appsettings.json`, `docker-compose.yml`, `.env` |
| `REPLACE_JWT_SECRET` | JWT signing secret — minimum 32 characters, random | `appsettings.json` |
| `REPLACE_JWT_ISSUER` | JWT issuer (e.g. `https://api.yourdomain.com`) | `appsettings.json` |
| `REPLACE_JWT_AUDIENCE` | JWT audience (e.g. `https://yourdomain.com`) | `appsettings.json` |
| `REPLACE_ADMIN_EMAIL` | Seed admin account email | `appsettings.Development.json` |
| `REPLACE_ADMIN_PASSWORD` | Seed admin account password | `appsettings.Development.json` |
| `REPLACE_ALLOWED_ORIGINS` | CORS origins (e.g. `http://localhost:4200`) | `appsettings.json` |
| `REPLACE_DOMAIN` | Your public domain for Nginx SSL config | `nginx.conf` |
| `REPLACE_SSL_CERT_PATH` | Path to SSL certificate on server | `nginx.conf` |
| `REPLACE_SSL_KEY_PATH` | Path to SSL private key on server | `nginx.conf` |
| `REPLACE_REGISTRY` | Docker registry (e.g. `ghcr.io/youruser`) | `.github/workflows/cd.yml` |
| `REPLACE_DEPLOY_HOST` | SSH host for deployment | `.github/workflows/cd.yml` |
| `REPLACE_DEPLOY_USER` | SSH user for deployment | `.github/workflows/cd.yml` |

### appsettings.json (template)

```json
{
  "ConnectionStrings": {
    "Default": "Host=REPLACE_DB_HOST;Port=REPLACE_DB_PORT;Database=REPLACE_DB_NAME;Username=REPLACE_DB_USER;Password=REPLACE_DB_PASSWORD"
  },
  "Jwt": {
    "Secret": "REPLACE_JWT_SECRET",
    "Issuer": "REPLACE_JWT_ISSUER",
    "Audience": "REPLACE_JWT_AUDIENCE",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Cors": {
    "AllowedOrigins": ["REPLACE_ALLOWED_ORIGINS"]
  },
  "RateLimit": {
    "RequestsPerMinute": 60
  }
}
```

### .env (for Docker Compose)

```env
DB_HOST=db
DB_PORT=5432
DB_NAME=REPLACE_DB_NAME
DB_USER=REPLACE_DB_USER
DB_PASSWORD=REPLACE_DB_PASSWORD
JWT_SECRET=REPLACE_JWT_SECRET
JWT_ISSUER=REPLACE_JWT_ISSUER
JWT_AUDIENCE=REPLACE_JWT_AUDIENCE
```

> **Security note:** Never commit `.env` or `appsettings.Production.json` to version control.
> The `.gitignore` excludes these files. Use GitHub Actions secrets or a secrets manager in production.

---

## Setup Instructions

### Option A — Docker Compose (Recommended)

This runs everything — API, PostgreSQL, Nginx — with a single command.

```bash
# 1. Clone the repository
git clone https://github.com/REPLACE_GITHUB_USER/ledger-system.git
cd ledger-system

# 2. Copy the environment template and fill in your values
cp .env.example .env
# Edit .env with your values (see placeholder table above)

# 3. Build and start all services
docker compose up --build

# 4. Run database migrations (first time only)
docker compose exec api dotnet ef database update \
  --project src/LedgerSystem.Infrastructure \
  --startup-project src/LedgerSystem.API

# 5. Seed the database (creates admin user + test wallets)
docker compose exec api dotnet run --project src/LedgerSystem.API -- --seed
```

The API is now available at `http://localhost:80`.  
Swagger UI is at `http://localhost:80/swagger`.

---

### Option B — Local Development (Without Docker)

#### 1. Create the PostgreSQL database

```bash
psql -U postgres
```

```sql
CREATE USER ledger_user WITH PASSWORD 'REPLACE_DB_PASSWORD';
CREATE DATABASE ledger_dev OWNER ledger_user;
GRANT ALL PRIVILEGES ON DATABASE ledger_dev TO ledger_user;
\q
```

#### 2. Configure the API

```bash
cd src/LedgerSystem.API
cp appsettings.json appsettings.Development.json
# Edit appsettings.Development.json and replace all REPLACE_ values
```

#### 3. Run migrations

```bash
dotnet ef database update \
  --project ../LedgerSystem.Infrastructure \
  --startup-project .
```

#### 4. Seed data

```bash
dotnet run -- --seed
```

#### 5. Start the API

```bash
dotnet run
```

API available at `https://localhost:5001`.  
Swagger UI at `https://localhost:5001/swagger`.

#### 6. (Optional) Start the Angular frontend

```bash
cd frontend/ledger-angular
npm install
ng serve
```

Frontend available at `http://localhost:4200`.

---

## Running the Project

### Docker Compose

```bash
# Start all services in background
docker compose up -d

# View logs
docker compose logs -f api

# Stop all services
docker compose down

# Stop and remove volumes (WARNING: deletes all data)
docker compose down -v
```

### Local

```bash
# API only
cd src/LedgerSystem.API
dotnet run

# API in watch mode (hot reload)
dotnet watch run

# Angular frontend
cd frontend/ledger-angular
ng serve --open
```

### Useful URLs

| Service | URL |
|---|---|
| API (Docker) | `http://localhost:80/api` |
| API (Local) | `https://localhost:5001/api` |
| Swagger UI | `/swagger` |
| Health check | `/health` |
| Angular (local) | `http://localhost:4200` |

---

## Running Tests

### All tests

```bash
dotnet test
```

### Unit tests only

```bash
dotnet test tests/LedgerSystem.UnitTests
```

### Integration tests only

Integration tests require a running PostgreSQL instance. By default they use the connection string in `appsettings.Test.json`.

```bash
# Using Docker for the test database
docker compose -f docker-compose.test.yml up -d

dotnet test tests/LedgerSystem.IntegrationTests

docker compose -f docker-compose.test.yml down -v
```

### With coverage report

```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
open coverage-report/index.html
```

### Concurrency tests (double-spend)

Concurrency tests are marked with `[Trait("Category", "Concurrency")]` and are included in the integration test suite. They spin up 10 parallel transfer requests against a single wallet and assert the final balance never goes negative.

```bash
dotnet test tests/LedgerSystem.IntegrationTests \
  --filter "Category=Concurrency" \
  --logger "console;verbosity=detailed"
```

---

## API Overview

Full documentation is available via Swagger at `/swagger` when the API is running.

### Authentication

```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
```

All protected endpoints require:
```
Authorization: Bearer <access_token>
```

### Wallets

```
GET    /api/wallets
POST   /api/wallets
GET    /api/wallets/{id}
GET    /api/wallets/{id}/history?page=1&pageSize=20
```

### Transfers

All mutating endpoints require:
```
Idempotency-Key: <uuid-v4>
```

```
POST   /api/transfers
GET    /api/transfers
GET    /api/transfers/{id}
```

**Transfer request:**
```json
{
  "sourceWalletId": "uuid",
  "destinationWalletId": "uuid",
  "amount": "150.00",
  "currency": "USD",
  "description": "Rent payment"
}
```

### Admin (finance / admin roles)

```
GET    /api/admin/users
GET    /api/admin/users/{id}/wallets
GET    /api/admin/ledger
POST   /api/admin/wallets/{id}/freeze
POST   /api/admin/wallets/{id}/unfreeze
```

### Error response format

All errors return a consistent envelope:
```json
{
  "error": {
    "code": "INSUFFICIENT_FUNDS",
    "message": "Source wallet has insufficient funds for this transfer",
    "traceId": "abc123"
  }
}
```

| Error Code | HTTP |
|---|---|
| `INSUFFICIENT_FUNDS` | 422 |
| `WALLET_NOT_FOUND` | 404 |
| `WALLET_FROZEN` | 422 |
| `CURRENCY_MISMATCH` | 422 |
| `SELF_TRANSFER` | 422 |
| `IDEMPOTENCY_KEY_REQUIRED` | 400 |
| `DUPLICATE_TRANSFER` | 200 (replayed) |
| `UNAUTHORIZED` | 401 |
| `FORBIDDEN` | 403 |

---

## Key Engineering Decisions

### 1. NUMERIC(19,4) for all money columns
Floating-point types (`float`, `double`) cannot represent many decimal values exactly. Financial calculations accumulate rounding errors. `NUMERIC(19,4)` stores exact decimal values. This is non-negotiable for a payment system.

### 2. Immutable ledger entries
Ledger entries are never updated or deleted. Any financial correction is made via a compensating entry (a new debit or credit that reverses the error). This gives a permanent, auditable record of every money movement. The `ledger_entries` table has no `updated_at` column to make this explicit.

### 3. ACID transactions with ordered wallet locking
The transfer operation wraps balance updates and ledger writes in a single PostgreSQL transaction. To prevent deadlocks under concurrent transfers, wallets are always locked in ascending UUID order using `SELECT FOR UPDATE`. Two concurrent transfers between wallets A↔B always lock A first, then B — so they queue rather than deadlock.

### 4. Idempotency keys
Clients must supply an `Idempotency-Key` UUID header on all mutating requests. The server stores the key and the full response. If the same key is received again within 24 hours, the stored response is returned immediately without re-executing the operation. This makes the API safe for clients to retry on network failure without risk of double-charging.

### 5. balance_after snapshot on ledger entries
Each ledger entry stores the wallet's balance immediately after that entry was applied. This allows point-in-time balance queries with a single indexed lookup (`WHERE created_at <= $timestamp ORDER BY created_at DESC LIMIT 1`) rather than replaying all prior entries.

### 6. UUID primary keys
All primary keys are UUID v4. This prevents enumeration attacks (an attacker cannot guess sequential IDs), works correctly in distributed/multi-instance deployments, and is the standard in financial systems.

### 7. Real PostgreSQL in integration tests — not SQLite
SQLite does not support `SELECT FOR UPDATE`, `NUMERIC` precision, or several PostgreSQL constraint behaviours. Tests that pass against SQLite can fail against PostgreSQL in production. All integration tests run against a real PostgreSQL instance (via Docker in CI).

### 8. Event sourcing alignment
The ledger is designed as an append-only event log — a natural fit for event sourcing. Current design stores state snapshots (`balance_after`) for query performance. A future migration to full event sourcing would replace direct balance mutations with domain events (`DebitApplied`, `CreditApplied`) and rebuild state via aggregate replay. The schema supports this transition without a destructive migration.

### 9. JWT with refresh token rotation
Access tokens have a 15-minute expiry to limit the blast radius of a leaked token. Refresh tokens (7-day expiry) are stored server-side in the database, enabling revocation. On each refresh, the old refresh token is invalidated and a new one is issued (rotation). Compromised refresh tokens can be detected via reuse detection.

---

## DevOps

### Docker Compose services

| Service | Description | Port |
|---|---|---|
| `api` | .NET 8 API | Internal only (proxied by Nginx) |
| `db` | PostgreSQL 16 | `5432` (internal) |
| `nginx` | Reverse proxy | `80`, `443` |

### CI/CD — GitHub Actions

**On every push / pull request:**
1. Restore dependencies
2. Build solution
3. Run unit tests
4. Spin up PostgreSQL service container
5. Run integration tests (including concurrency tests)
6. Build Docker image

**On merge to `main`:**
1. All CI steps above
2. Push Docker image to registry (`REPLACE_REGISTRY`)
3. SSH to deploy host (`REPLACE_DEPLOY_HOST`) and pull + restart

### Required GitHub Actions secrets

Set these in `Settings > Secrets and variables > Actions`:

| Secret name | Value |
|---|---|
| `DB_PASSWORD` | Production database password |
| `JWT_SECRET` | Production JWT signing secret |
| `DOCKER_REGISTRY_TOKEN` | Registry push token |
| `DEPLOY_SSH_KEY` | Private SSH key for deploy host |
| `DEPLOY_HOST` | Deploy server hostname or IP |
| `DEPLOY_USER` | SSH username on deploy server |

### Nginx SSL setup

Replace the SSL placeholders in `nginx.conf`:

```nginx
ssl_certificate     REPLACE_SSL_CERT_PATH;   # e.g. /etc/letsencrypt/live/yourdomain/fullchain.pem
ssl_certificate_key REPLACE_SSL_KEY_PATH;    # e.g. /etc/letsencrypt/live/yourdomain/privkey.pem
server_name         REPLACE_DOMAIN;          # e.g. api.yourdomain.com
```

For local development, SSL is not required. The `docker-compose.yml` exposes HTTP on port 80 only.

---

## Development Checklist

- [ ] **M1** — Solution structure + domain entities
- [ ] **M2** — Database schema + EF Core migrations + seed data
- [ ] **M3** — JWT authentication (register / login / refresh)
- [ ] **M4** — Wallet management endpoints
- [ ] **M5** — Transfer service + double-entry ledger (ACID)
- [ ] **M6** — Idempotency middleware + global error handler + rate limiting
- [ ] **M7** — RBAC policies + admin endpoints
- [ ] **M8** — Unit tests + integration tests + concurrency tests
- [ ] **M9** — Docker + Docker Compose + Nginx + GitHub Actions CI/CD

---

## License

MIT
