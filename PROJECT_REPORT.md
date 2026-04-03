# Fintech Ledger System — Full Project Report

**Stack:** C# / .NET 8 | Angular | PostgreSQL  
**Domain:** Financial Technology — Double-Entry Ledger & Payment System  
**Architecture:** Clean/Layered Architecture  

---

## 1. Project Overview

A production-grade, double-entry ledger-based payment system that tracks money movement across user wallets using immutable ledger entries. The system enforces strict financial consistency through ACID transactions, prevents double-spending via idempotency, and exposes a secure RESTful API with role-based access control.

### Why This Domain

Double-entry bookkeeping is the accounting standard used by every bank, payment processor, and financial institution. Every debit must have a corresponding credit. No money is created or destroyed — it only moves. Building this forces you to solve real engineering problems:

- ACID transactions across multiple tables
- Race conditions and concurrent transfer handling
- Immutable audit trails
- Idempotent payment operations
- Role-based access to sensitive financial data

---

## 2. Core Features

| Feature | Description |
|---|---|
| User Accounts | Registration, authentication, profile management |
| Wallets | Each user has one or more wallets with a currency and balance |
| Transactions | Debit/credit operations recorded atomically |
| Ledger Entries | Immutable double-entry records — never updated, never deleted |
| Transfers | Move funds between wallets with full consistency guarantees |
| Idempotency | Duplicate transfer requests are safely deduplicated |
| Balance API | Real-time and historical balance queries |
| Transaction History | Paginated, filterable ledger history per wallet |
| RBAC | Admin / User / Finance Officer roles with scoped permissions |

---

## 3. System Architecture

### 3.1 Layered / Clean Architecture

```
LedgerSystem/
├── LedgerSystem.API/               # HTTP layer — controllers, middleware, filters
├── LedgerSystem.Application/       # Use cases — services, DTOs, interfaces
├── LedgerSystem.Domain/            # Core business rules — entities, domain events
├── LedgerSystem.Infrastructure/    # DB, external services, repositories
└── LedgerSystem.Tests/             # Unit, integration, end-to-end tests
```

**Dependency direction:** API → Application → Domain ← Infrastructure

The domain layer has zero dependencies on frameworks or databases. This makes the core business logic independently testable and framework-agnostic.

### 3.2 Request Flow

```
HTTP Request
    → Middleware (auth, rate limiting, logging, validation)
    → Controller (route handler, maps request to command/query)
    → Application Service (orchestrates use case)
    → Domain Entity (enforces business rules)
    → Repository (persists via EF Core)
    → PostgreSQL
```

---

## 4. Database Design

### 4.1 Entity Relationship Overview

```
Users (1) ──── (N) Wallets (1) ──── (N) LedgerEntries
                                              │
Transfers (1) ─────────────────────── (2) LedgerEntries
                                              │
IdempotencyKeys (1) ──────────────── (1) Transfers
```

### 4.2 Core Tables

#### `users`
```sql
id              UUID PRIMARY KEY DEFAULT gen_random_uuid()
email           VARCHAR(255) UNIQUE NOT NULL
password_hash   TEXT NOT NULL
role            VARCHAR(50) NOT NULL DEFAULT 'user'   -- user | admin | finance
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
```

#### `wallets`
```sql
id              UUID PRIMARY KEY DEFAULT gen_random_uuid()
user_id         UUID NOT NULL REFERENCES users(id)
currency        CHAR(3) NOT NULL DEFAULT 'USD'
balance         NUMERIC(19, 4) NOT NULL DEFAULT 0
is_active       BOOLEAN NOT NULL DEFAULT true
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()

CONSTRAINT balance_non_negative CHECK (balance >= 0)
INDEX idx_wallets_user_id ON wallets(user_id)
```

#### `ledger_entries`
```sql
id              UUID PRIMARY KEY DEFAULT gen_random_uuid()
wallet_id       UUID NOT NULL REFERENCES wallets(id)
transfer_id     UUID REFERENCES transfers(id)
entry_type      VARCHAR(10) NOT NULL               -- DEBIT | CREDIT
amount          NUMERIC(19, 4) NOT NULL
balance_after   NUMERIC(19, 4) NOT NULL
description     TEXT
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()

-- Ledger entries are NEVER updated or deleted
-- No updated_at column by design
CONSTRAINT amount_positive CHECK (amount > 0)
INDEX idx_ledger_wallet_id ON ledger_entries(wallet_id)
INDEX idx_ledger_created_at ON ledger_entries(created_at DESC)
```

#### `transfers`
```sql
id                  UUID PRIMARY KEY DEFAULT gen_random_uuid()
source_wallet_id    UUID NOT NULL REFERENCES wallets(id)
dest_wallet_id      UUID NOT NULL REFERENCES wallets(id)
amount              NUMERIC(19, 4) NOT NULL
currency            CHAR(3) NOT NULL
status              VARCHAR(20) NOT NULL DEFAULT 'pending'  -- pending | completed | failed
idempotency_key     VARCHAR(255) UNIQUE NOT NULL
description         TEXT
created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
completed_at        TIMESTAMPTZ

CONSTRAINT amount_positive CHECK (amount > 0)
CONSTRAINT different_wallets CHECK (source_wallet_id != dest_wallet_id)
INDEX idx_transfers_idempotency ON transfers(idempotency_key)
INDEX idx_transfers_source ON transfers(source_wallet_id)
INDEX idx_transfers_dest ON transfers(dest_wallet_id)
```

#### `idempotency_keys`
```sql
key             VARCHAR(255) PRIMARY KEY
user_id         UUID NOT NULL REFERENCES users(id)
request_path    TEXT NOT NULL
response_status INT NOT NULL
response_body   JSONB NOT NULL
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
expires_at      TIMESTAMPTZ NOT NULL DEFAULT NOW() + INTERVAL '24 hours'

INDEX idx_idempotency_expires ON idempotency_keys(expires_at)
```

### 4.3 Design Decisions

**NUMERIC(19,4) not FLOAT** — Floating point arithmetic is unsuitable for money. `NUMERIC` is exact.

**UUID primary keys** — Prevents enumeration attacks and works well in distributed systems.

**`balance_after` on ledger entries** — Allows point-in-time balance reconstruction without replaying all entries. A query for "what was my balance at date X" is a single indexed lookup.

**No soft deletes on ledger entries** — Ledger entries are immutable by design. Once written, they cannot be modified or removed. Any correction is made via a compensating entry.

**`idempotency_key` on transfers** — Clients must supply this. The server uses it to detect and replay duplicate requests safely.

---

## 5. Idempotency

### Why It Matters

In payment systems, networks fail. A client sends a transfer request, the server processes it, but the response never arrives. The client retries. Without idempotency, the money moves twice.

Idempotency guarantees: **the same operation, sent multiple times, has the same effect as sending it once.**

### Implementation

Every mutating request (transfers, wallet creation) requires an `Idempotency-Key` header. This is a UUID generated by the client and stored with the response.

```
POST /api/transfers
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
```

**Server behaviour:**

1. Hash the idempotency key + user ID
2. Check `idempotency_keys` table for an existing record
3. **If found:** return the stored response immediately — do not re-execute
4. **If not found:** execute the transfer, store key + full response, return result

```csharp
// Application/Services/IdempotencyService.cs
public async Task<IdempotencyResult?> GetCachedResponse(string key, Guid userId)
{
    return await _repository.FindAsync(key, userId);
}

public async Task StoreResponse(string key, Guid userId, int statusCode, object body)
{
    var entry = new IdempotencyKey
    {
        Key = key,
        UserId = userId,
        ResponseStatus = statusCode,
        ResponseBody = JsonSerializer.Serialize(body),
        ExpiresAt = DateTime.UtcNow.AddHours(24)
    };
    await _repository.InsertAsync(entry);
}
```

**Middleware integration:**

```csharp
// API/Middleware/IdempotencyMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    if (!HttpMethods.IsPost(context.Request.Method) &&
        !HttpMethods.IsPut(context.Request.Method))
    {
        await _next(context);
        return;
    }

    var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
    if (string.IsNullOrEmpty(key))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = "Idempotency-Key header required" });
        return;
    }

    var userId = context.GetUserId();
    var cached = await _idempotencyService.GetCachedResponse(key, userId);

    if (cached != null)
    {
        context.Response.StatusCode = cached.ResponseStatus;
        await context.Response.WriteAsync(cached.ResponseBody);
        return;
    }

    await _next(context);
    // Store response after execution
}
```

### Expiry

Idempotency keys expire after 24 hours. A background job (Hangfire or .NET `IHostedService`) purges expired keys daily. After expiry, the same key from the same client is treated as a new request.

---

## 6. Event Sourcing Consideration

### What Is Event Sourcing

Traditional systems store the **current state** of an entity. Event sourcing stores the **sequence of events** that led to the current state. The state is derived by replaying events.

```
Traditional: wallets.balance = 850.00

Event Sourced:
  WalletCreated         { balance: 0 }
  FundsDeposited        { amount: 1000 }
  TransferDebited       { amount: 150 }
  → Replay → balance: 850.00
```

### Why Ledgers Naturally Map to Event Sourcing

A double-entry ledger **already is** an event log. Each ledger entry is an immutable event. The current balance is the sum of all credits minus all debits. This project implements the key properties of event sourcing:

| Event Sourcing Property | This Project's Equivalent |
|---|---|
| Immutable event log | `ledger_entries` — never updated or deleted |
| State derived from events | Balance = SUM of ledger entries per wallet |
| Point-in-time queries | Query ledger entries up to a given timestamp |
| Audit trail by default | Every money movement is permanently recorded |

### Current Design (Event Sourcing Lite)

The project stores `balance_after` as a snapshot on each ledger entry — a performance optimisation. This means you can answer "what was the balance at time T?" with a single query rather than replaying all prior entries.

```sql
SELECT balance_after
FROM ledger_entries
WHERE wallet_id = $1
  AND created_at <= $2
ORDER BY created_at DESC
LIMIT 1;
```

### Full Event Sourcing (Future Extension)

A full event sourcing implementation would:

1. Replace direct balance updates with domain events (`TransferInitiated`, `DebitApplied`, `CreditApplied`, `TransferCompleted`)
2. Store events in an append-only `domain_events` table
3. Rebuild wallet state by replaying events through an aggregate
4. Use projections to maintain read-optimised views (e.g. current balances)

This would also enable:
- **Event-driven architecture** — downstream services subscribe to `TransferCompleted` events
- **CQRS** — separate read/write models
- **Time travel debugging** — reconstruct exact system state at any point

This is acknowledged as a natural evolution of the current design, not a requirement for v1.

---

## 7. API Design

### 7.1 Authentication & Authorization

JWT-based authentication. Tokens contain user ID and role claims.

```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
```

### 7.2 Wallets

```
GET    /api/wallets                    # List my wallets
POST   /api/wallets                    # Create wallet
GET    /api/wallets/{id}               # Get wallet details + balance
GET    /api/wallets/{id}/history       # Paginated ledger history
```

### 7.3 Transfers

```
POST   /api/transfers                  # Initiate transfer (requires Idempotency-Key)
GET    /api/transfers/{id}             # Get transfer status
GET    /api/transfers                  # List my transfers (paginated)
```

### 7.4 Admin (Finance / Admin roles only)

```
GET    /api/admin/users                # List all users
GET    /api/admin/users/{id}/wallets   # View any user's wallets
GET    /api/admin/ledger               # Full ledger audit view
POST   /api/admin/wallets/{id}/freeze  # Freeze a wallet
```

### 7.5 Transfer Request/Response

**Request:**
```json
POST /api/transfers
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{
  "sourceWalletId": "uuid",
  "destinationWalletId": "uuid",
  "amount": "150.00",
  "currency": "USD",
  "description": "Rent payment"
}
```

**Response:**
```json
HTTP 201 Created

{
  "transferId": "uuid",
  "status": "completed",
  "sourceWallet": { "id": "uuid", "balanceAfter": "850.0000" },
  "destinationWallet": { "id": "uuid", "balanceAfter": "1150.0000" },
  "amount": "150.0000",
  "currency": "USD",
  "completedAt": "2026-04-03T15:00:00Z"
}
```

### 7.6 Error Responses

All errors follow a consistent envelope:

```json
{
  "error": {
    "code": "INSUFFICIENT_FUNDS",
    "message": "Source wallet has insufficient funds for this transfer",
    "traceId": "abc123"
  }
}
```

| Code | HTTP Status |
|---|---|
| `INSUFFICIENT_FUNDS` | 422 |
| `WALLET_NOT_FOUND` | 404 |
| `WALLET_FROZEN` | 422 |
| `IDEMPOTENCY_KEY_REQUIRED` | 400 |
| `DUPLICATE_TRANSFER` | 200 (replayed) |
| `CURRENCY_MISMATCH` | 422 |
| `SELF_TRANSFER` | 422 |

---

## 8. Transfer Service — Core Logic

The transfer operation is the most critical piece. It must be atomic across wallet balance updates and ledger entry creation.

```csharp
// Application/Services/TransferService.cs
public async Task<TransferResult> ExecuteTransferAsync(TransferCommand command)
{
    await using var transaction = await _dbContext.Database.BeginTransactionAsync(
        IsolationLevel.ReadCommitted);

    try
    {
        // 1. Lock wallets in consistent order to prevent deadlocks
        var (source, destination) = await LockWalletsAsync(
            command.SourceWalletId,
            command.DestinationWalletId);

        // 2. Validate
        if (source.Balance < command.Amount)
            throw new InsufficientFundsException(source.Id);

        if (source.Currency != destination.Currency)
            throw new CurrencyMismatchException();

        // 3. Debit source
        source.Balance -= command.Amount;
        var debitEntry = LedgerEntry.CreateDebit(source, command.Amount, command.TransferId);

        // 4. Credit destination
        destination.Balance += command.Amount;
        var creditEntry = LedgerEntry.CreateCredit(destination, command.Amount, command.TransferId);

        // 5. Record transfer
        var transfer = Transfer.Complete(command, debitEntry, creditEntry);

        // 6. Persist all in one transaction
        _dbContext.LedgerEntries.AddRange(debitEntry, creditEntry);
        _dbContext.Transfers.Add(transfer);
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return TransferResult.Success(transfer, source, destination);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

// Wallet locking in deterministic order prevents deadlocks under concurrency
private async Task<(Wallet source, Wallet dest)> LockWalletsAsync(Guid sourceId, Guid destId)
{
    // Always lock the wallet with the lower UUID first
    var ids = new[] { sourceId, destId }.OrderBy(id => id).ToArray();

    var wallets = await _dbContext.Wallets
        .FromSqlRaw("SELECT * FROM wallets WHERE id = ANY(@ids) FOR UPDATE", 
            new NpgsqlParameter("ids", ids))
        .ToListAsync();

    return (
        wallets.Single(w => w.Id == sourceId),
        wallets.Single(w => w.Id == destId)
    );
}
```

---

## 9. RBAC Design

Three roles with scoped permissions:

| Permission | User | Finance Officer | Admin |
|---|---|---|---|
| View own wallets | Y | Y | Y |
| Initiate transfers | Y | Y | Y |
| View own history | Y | Y | Y |
| View any user's data | N | Y | Y |
| View full ledger | N | Y | Y |
| Freeze wallets | N | N | Y |
| Manage users | N | N | Y |

Implemented via ASP.NET Core policy-based authorization:

```csharp
// Startup / Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FinanceOrAbove", policy =>
        policy.RequireRole("finance", "admin"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));
});
```

---

## 10. DevOps

### 10.1 Docker Compose (Development)

```yaml
services:
  api:
    build: .
    ports: ["5000:8080"]
    environment:
      - ConnectionStrings__Default=Host=db;Database=ledger;Username=app;Password=secret
    depends_on: [db]

  db:
    image: postgres:16
    environment:
      POSTGRES_DB: ledger
      POSTGRES_USER: app
      POSTGRES_PASSWORD: secret
    volumes:
      - pgdata:/var/lib/postgresql/data

  nginx:
    image: nginx:alpine
    ports: ["80:80", "443:443"]
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on: [api]
```

### 10.2 CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/ci.yml
on: [push, pull_request]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: ledger_test
          POSTGRES_PASSWORD: test
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --logger trx

  docker-build:
    needs: build-and-test
    runs-on: ubuntu-latest
    steps:
      - run: docker build -t ledger-system:${{ github.sha }} .
```

### 10.3 Environments

| Environment | Purpose | DB |
|---|---|---|
| Development | Local dev, hot reload | Docker Compose Postgres |
| Test | CI pipeline, integration tests | Ephemeral Postgres in CI |
| Staging | Pre-production validation | Managed Postgres (cloud) |
| Production | Live system | Managed Postgres + connection pooling (PgBouncer) |

---

## 11. Testing Strategy

### 11.1 Unit Tests (xUnit)

Target: domain logic and application services in isolation. Mock all I/O.

```csharp
[Fact]
public void Transfer_ShouldThrow_WhenInsufficientFunds()
{
    var wallet = Wallet.Create(userId: Guid.NewGuid(), currency: "USD");
    wallet.Credit(100m);

    var act = () => wallet.Debit(200m);

    act.Should().Throw<InsufficientFundsException>();
}
```

### 11.2 Integration Tests (xUnit + WebApplicationFactory)

Spin up the full application in memory against a real PostgreSQL test database. Test the full HTTP stack — routing, middleware, auth, database.

```csharp
public class TransferEndpointTests : IClassFixture<LedgerWebApplicationFactory>
{
    [Fact]
    public async Task PostTransfer_ShouldReturn201_WhenValid()
    {
        var client = _factory.CreateAuthenticatedClient(role: "user");

        var response = await client.PostAsJsonAsync("/api/transfers", new
        {
            sourceWalletId = _seededSourceWalletId,
            destinationWalletId = _seededDestWalletId,
            amount = "50.00",
            currency = "USD"
        }, withIdempotencyKey: Guid.NewGuid().ToString());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostTransfer_ShouldReturn200_WhenDuplicateIdempotencyKey()
    {
        var key = Guid.NewGuid().ToString();
        var client = _factory.CreateAuthenticatedClient(role: "user");

        await client.PostTransfer(key);
        var duplicate = await client.PostTransfer(key); // same key

        duplicate.StatusCode.Should().Be(HttpStatusCode.OK); // replayed, not re-executed
    }
}
```

### 11.3 Concurrency Tests

Test that double-spending is impossible under concurrent load:

```csharp
[Fact]
public async Task ConcurrentTransfers_ShouldNotOverdraw()
{
    // Wallet starts with 100.00
    // 10 concurrent requests each try to transfer 20.00
    // Only 5 should succeed — total debited must not exceed 100.00

    var tasks = Enumerable.Range(0, 10)
        .Select(_ => _client.PostTransfer(amount: 20m, key: Guid.NewGuid().ToString()));

    var results = await Task.WhenAll(tasks);

    var succeeded = results.Count(r => r.StatusCode == HttpStatusCode.Created);
    var finalBalance = await GetWalletBalance(_sourceWalletId);

    succeeded.Should().Be(5);
    finalBalance.Should().Be(0m);
}
```

### 11.4 Test Coverage Targets

| Layer | Target Coverage |
|---|---|
| Domain entities | 95%+ |
| Application services | 85%+ |
| API endpoints (integration) | 80%+ |
| Concurrency scenarios | All critical paths |

---

## 12. Angular Frontend (Phase 2)

Recommended to build after the API layer is stable and tested.

### Planned Views

| View | Description |
|---|---|
| Dashboard | Balance summary across all wallets |
| Wallet Detail | Ledger history, paginated and filterable |
| Send Money | Transfer form with amount, recipient, description |
| Transaction Detail | Single transfer with debit/credit entries |
| Admin Panel | User management, wallet freeze, full ledger audit |

### Key Implementation Notes

- Use Angular `HttpInterceptor` to attach JWT and `Idempotency-Key` headers to all mutating requests
- Generate idempotency keys client-side using `crypto.randomUUID()` before form submission
- Use Angular Signals or NgRx for wallet balance state
- Optimistic UI updates on transfer submission with rollback on failure

---

## 13. Project Phases

### Phase 1 — Core API (Backend Complete)
- [ ] Solution structure + layered architecture
- [ ] PostgreSQL schema + migrations (EF Core)
- [ ] User auth (JWT)
- [ ] Wallet CRUD
- [ ] Transfer service with ACID transaction + wallet locking
- [ ] Idempotency middleware
- [ ] RBAC policies
- [ ] Error handling middleware
- [ ] Swagger/OpenAPI docs
- [ ] Unit tests — domain + services
- [ ] Integration tests — all endpoints
- [ ] Concurrency tests — double-spend prevention

### Phase 2 — DevOps
- [ ] Dockerfile + Docker Compose
- [ ] Nginx reverse proxy config
- [ ] GitHub Actions CI pipeline
- [ ] Environment configuration (dev / test / prod)
- [ ] Logging (Serilog + structured logs)
- [ ] Health check endpoints

### Phase 3 — Angular Frontend
- [ ] Auth flow (login, register, token refresh)
- [ ] Wallet dashboard
- [ ] Transfer form with idempotency
- [ ] Ledger history view
- [ ] Admin panel

### Phase 4 — Advanced (Optional)
- [ ] Event sourcing migration for `ledger_entries`
- [ ] Domain events + outbox pattern
- [ ] Kubernetes deployment manifests
- [ ] Terraform infrastructure as code
- [ ] PgBouncer connection pooling
- [ ] Distributed tracing (OpenTelemetry)

---

## 14. Key Engineering Decisions Summary

| Decision | Choice | Reason |
|---|---|---|
| Money representation | `NUMERIC(19,4)` | Exact arithmetic — never use float for money |
| Primary keys | UUID v4 | Prevents enumeration, distributed-friendly |
| Concurrency control | `SELECT FOR UPDATE` with ordered locking | Prevents deadlocks under concurrent transfers |
| Idempotency | Client-supplied key + server-side dedup table | Industry standard for payment APIs |
| Ledger mutability | Append-only, no updates/deletes | Audit correctness, event sourcing alignment |
| Balance storage | Denormalised on wallet + snapshot on ledger entry | Query performance without sacrificing auditability |
| Auth | JWT with refresh tokens | Stateless, works well with Angular SPA |
| Test database | Real Postgres in CI (not SQLite) | Tests must match production behaviour |
