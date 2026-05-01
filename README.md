# Ledger System

![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Angular](https://img.shields.io/badge/Angular-17-DD0031?logo=angular)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)
![Tests](https://img.shields.io/badge/Tests-xUnit-512BD4?logo=.net)
![Domain Coverage](https://img.shields.io/badge/Domain-95%25-brightgreen)
![Application Coverage](https://img.shields.io/badge/Application-85%25-green)
![Integration Coverage](https://img.shields.io/badge/Integration-80%25-green)

Double-entry ledger-based payment API built with .NET 8, PostgreSQL, and Angular. Tracks money movement across user wallets using immutable ledger entries, ACID transactions, and idempotent transfer endpoints.

---

## Table of Contents

1. [Tech Stack](#tech-stack)
2. [Project Structure](#project-structure)
3. [Prerequisites](#prerequisites)
4. [Setup Instructions](#setup-instructions)
5. [Running the Project](#running-the-project)
6. [Running Tests](#running-tests)
7. [API Overview](#api-overview)
8. [Key Engineering Decisions](#key-engineering-decisions)
9. [DevOps](#devops)

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
│   └── ledger-angular/
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

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 8.x | https://dotnet.microsoft.com/download |
| PostgreSQL | 16.x | https://www.postgresql.org/download |
| Docker | 24.x+ | https://docs.docker.com/get-docker |
| Docker Compose | v2.x | Bundled with Docker Desktop |
| Node.js | 20.x LTS | https://nodejs.org |
| Angular CLI | 17.x | `npm install -g @angular/cli` |
| EF Core CLI | 8.x | `dotnet tool install --global dotnet-ef` |

---

## Setup Instructions

**1. Install EF tool** *(once per machine)*

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef --version
```

**2. Restore & build**

```bash
dotnet restore
dotnet build
```

**3. Ensure PostgreSQL is running**

```bash
sudo service postgresql status
# If stopped:
sudo service postgresql start
```

**4. Enable pgcrypto** *(PostgreSQL 12 and below)*

```bash
sudo -u postgres psql -d ledger_dev -c 'CREATE EXTENSION IF NOT EXISTS "pgcrypto";'
```

**5. Apply database migrations**

```bash
dotnet ef database update \
  --project src/LedgerSystem.Infrastructure \
  --startup-project src/LedgerSystem.API
```

**6. Run the API**

Set `ASPNETCORE_ENVIRONMENT=Development` to expose Swagger UI.

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/LedgerSystem.API
```

**7. (Optional) Seed data**

```bash
dotnet run -- --seed
```

**8. (Optional) Start the Angular frontend**

```bash
cd frontend/ledger-angular
npm install
ng serve
```

Frontend available at `http://localhost:4200`.

### One-shot quick start

If tools are installed and the database already exists:

```bash
dotnet restore && \
dotnet build && \
dotnet ef database update \
  --project src/LedgerSystem.Infrastructure \
  --startup-project src/LedgerSystem.API && \
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/LedgerSystem.API
```

---

## Running the Project

| Service | URL |
|---|---|
| API (Docker) | `http://localhost:80/api` |
| API (local) | `https://localhost:5001/api` |
| Swagger UI | `/swagger` |
| Health check | `/health` |
| Angular | `http://localhost:4200` |

---

## Running Tests

**All tests**

```bash
dotnet test
```

**Unit tests only**

```bash
dotnet test tests/LedgerSystem.UnitTests
```

**Integration tests**

Integration tests require a running PostgreSQL instance. Connection string is read from `appsettings.Test.json`.

```bash
docker compose -f docker-compose.test.yml up -d
dotnet test tests/LedgerSystem.IntegrationTests
docker compose -f docker-compose.test.yml down -v
```

**Concurrency tests** *(double-spend)*

Marked with `[Trait("Category", "Concurrency")]`. Spins up 10 parallel transfer requests against a single wallet and asserts the final balance never goes negative.

```bash
dotnet test tests/LedgerSystem.IntegrationTests \
  --filter "Category=Concurrency" \
  --logger "console;verbosity=detailed"
```

**Coverage report**

```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
open coverage-report/index.html
```

---

## API Overview

Full documentation is available via Swagger at `/swagger` when the API is running.

All protected endpoints require:

```
Authorization: Bearer <access_token>
```

All mutating endpoints require:

```
Idempotency-Key: <uuid-v4>
```

### Authentication

```
POST  /api/auth/register
POST  /api/auth/login
POST  /api/auth/refresh
POST  /api/auth/logout
```

### Wallets

```
GET   /api/wallets
POST  /api/wallets
GET   /api/wallets/{id}
GET   /api/wallets/{id}/history?page=1&pageSize=20
```

### Transfers

```
POST  /api/transfers
GET   /api/transfers
GET   /api/transfers/{id}
```

Transfer request body:

```json
{
  "sourceWalletId": "uuid",
  "destinationWalletId": "uuid",
  "amount": "150.00",
  "currency": "USD",
  "description": "Rent payment"
}
```

### Admin *(finance / admin roles)*

```
GET   /api/admin/users
GET   /api/admin/users/{id}/wallets
GET   /api/admin/ledger
POST  /api/admin/wallets/{id}/freeze
POST  /api/admin/wallets/{id}/unfreeze
```

### Error response format

```json
{
  "error": {
    "code": "INSUFFICIENT_FUNDS",
    "message": "Source wallet has insufficient funds for this transfer",
    "traceId": "abc123"
  }
}
```

| Error code | HTTP |
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

**1. NUMERIC(19,4) for all money columns**

Float/double types cannot represent many decimal values exactly and accumulate rounding errors. `NUMERIC(19,4)` stores exact decimal values — non-negotiable for a payment system.

**2. Immutable ledger entries**

Ledger entries are never updated or deleted. Financial corrections are made via compensating entries. The `ledger_entries` table has no `updated_at` column to make this explicit.

**3. ACID transactions with ordered wallet locking**

The transfer operation wraps balance updates and ledger writes in a single PostgreSQL transaction. To prevent deadlocks under concurrent transfers, wallets are always locked in ascending UUID order using `SELECT FOR UPDATE`. Two concurrent A↔B transfers always lock A first — they queue rather than deadlock.

**4. Idempotency keys**

Clients supply an `Idempotency-Key` UUID header on all mutating requests. The server stores the key and full response for 24 hours. If the same key is received again, the stored response is returned without re-executing — safe for retries without risk of double-charging.

**5. balance_after snapshot on ledger entries**

Each ledger entry stores the wallet's balance after that entry was applied. This enables point-in-time balance queries with a single indexed lookup rather than replaying all prior entries.

**6. UUID primary keys**

Prevents enumeration attacks, works correctly in distributed deployments, and is standard in financial systems.

**7. Real PostgreSQL in integration tests**

SQLite lacks `SELECT FOR UPDATE`, `NUMERIC` precision, and several PostgreSQL constraint behaviours. Tests that pass against SQLite can fail against PostgreSQL in production. All integration tests run against real PostgreSQL via Docker in CI.

**8. Event sourcing alignment**

The append-only ledger is designed for a future migration to full event sourcing (`DebitApplied`, `CreditApplied`). The schema supports this transition without a destructive migration.

**9. JWT with refresh token rotation**

Access tokens expire after 15 minutes to limit blast radius. Refresh tokens (7-day expiry) are stored server-side for revocation. On each refresh the old token is invalidated. Reuse detection flags compromised tokens.

---

## DevOps

### Docker Compose services

| Service | Description | Port |
|---|---|---|
| `api` | .NET 8 API | Internal only (proxied by Nginx) |
| `db` | PostgreSQL 16 | 5432 (internal) |
| `nginx` | Reverse proxy | 80, 443 |

### CI/CD — GitHub Actions

On every push / pull request:

1. Restore dependencies
2. Build solution
3. Run unit tests
4. Spin up PostgreSQL service container
5. Run integration tests (including concurrency tests)
6. Build Docker image

On merge to `main`:

1. All CI steps above
2. Push Docker image to registry
3. SSH to deploy host and pull + restart

### Required GitHub Actions secrets

Set these in **Settings → Secrets and variables → Actions**:

| Secret | Value |
|---|---|
| `DB_PASSWORD` | Production database password |
| `JWT_SECRET` | Production JWT signing secret |
| `DOCKER_REGISTRY_TOKEN` | Registry push token |
| `DEPLOY_SSH_KEY` | Private SSH key for deploy host |
| `DEPLOY_HOST` | Deploy server hostname or IP |
| `DEPLOY_USER` | SSH username on deploy server |

### Nginx SSL setup

```nginx
ssl_certificate     /etc/letsencrypt/live/yourdomain/fullchain.pem;
ssl_certificate_key /etc/letsencrypt/live/yourdomain/privkey.pem;
server_name         api.yourdomain.com;
```

SSL is not required for local development — `docker-compose.yml` exposes HTTP on port 80 only.