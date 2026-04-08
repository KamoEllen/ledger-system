# Technical Report: Ledger System

**Stack:** .NET 8 / ASP.NET Core, PostgreSQL 16, Angular 17  
**Domain:** Financial Technology — Double-Entry Ledger and Payment API  
**Date:** April 2026

---

## Table of Contents

1. [Summary](#1-summary)
2. [Purpose and Requirements](#2-purpose-and-requirements)
3. [System Design and Choices](#3-system-design-and-choices)
4. [Comparing Different Methods](#4-comparing-different-methods)
5. [Testing and Data Results](#5-testing-and-data-results)
6. [Access and Security](#6-access-and-security)
7. [Conclusion](#7-conclusion)

---

## 1. Summary

This report covers the design and implementation of a double-entry ledger payment system built on .NET 8 and PostgreSQL. The system lets users hold wallets and transfer money between them. Every transfer is recorded as two immutable ledger entries — a debit and a credit — so the books always balance and a full audit trail is always available.

The system handles the core problems of financial software: preventing double-spending under concurrent requests, making the API safe to retry without charging twice, and enforcing strict rules about who can see or change financial data. It is structured in four layers (Domain, Application, Infrastructure, API), containerised with Docker, and deployed through a GitHub Actions pipeline.

---

## 2. Purpose and Requirements

### What the System Does

Users register, create one or more currency wallets, and transfer funds between wallets. Every balance change is recorded in an append-only ledger. Staff with elevated roles can view full system data and freeze wallets.

### Functional Requirements

- Register and authenticate users
- Create wallets (each tied to a single currency)
- Transfer funds between wallets atomically
- Record every balance change as a permanent ledger entry
- Return paginated transaction history per wallet
- Allow admin and finance roles to audit all accounts and freeze wallets
- Replay duplicate transfer requests safely without charging twice

### Non-Functional Requirements

- **Correctness:** No transfer can put a wallet balance below zero, even under concurrent load
- **Consistency:** Balance updates and ledger writes happen in a single database transaction — they succeed together or roll back together
- **Auditability:** Ledger entries are never modified or deleted; corrections use compensating entries
- **Reliability:** Clients can retry any mutating request without causing duplicate charges
- **Security:** All endpoints require authentication; admin and finance endpoints require role claims

---

## 3. System Design and Choices

### Layered Architecture

The codebase is split into four projects with a strict dependency direction:

```
API → Application → Domain ← Infrastructure
```

The Domain layer contains entities, value objects, and domain exceptions. It has no dependency on Entity Framework, ASP.NET Core, or any external library. This means domain logic can be tested in isolation without standing up a database or HTTP server.

The Application layer holds use-case services and interfaces. The Infrastructure layer implements those interfaces using EF Core and PostgreSQL. The API layer handles HTTP routing, middleware, and request mapping.

### Database Schema

The central tables are `wallets`, `ledger_entries`, `transfers`, and `idempotency_keys`.

All money columns use `NUMERIC(19,4)`. This is exact decimal arithmetic. Floating-point types (`float`, `double`) cannot represent many decimal values precisely and accumulate rounding errors — unacceptable in financial calculations.

All primary keys are UUID v4. Sequential integer IDs can be guessed by an attacker to enumerate resources. UUIDs also work correctly across distributed deployments without collision.

The `ledger_entries` table has no `updated_at` column by design. Once written, a ledger entry cannot be changed. The table carries a `balance_after` column that records the wallet balance immediately after each entry was applied. This lets the system answer point-in-time balance queries with a single indexed lookup rather than summing all prior entries.

### Transfer Execution

A transfer touches two wallets and creates two ledger entries. All four writes happen inside one PostgreSQL transaction at `ReadCommitted` isolation. If any step fails, the whole transaction rolls back — no partial state is ever committed.

To prevent deadlocks under concurrent transfers, the system always locks wallets in ascending UUID order using `SELECT FOR UPDATE`. If two requests try to transfer between wallet A and wallet B simultaneously, both requests lock A first, then B. They queue rather than deadlock.

### Idempotency

Every mutating endpoint (transfers, wallet creation) requires an `Idempotency-Key` UUID header supplied by the client. The server stores the key and the full response body. If the same key arrives again within 24 hours, the stored response is returned immediately — the operation is not re-executed. This makes the API safe to retry on network failure without risk of double-charging.

After 24 hours the key expires and the same UUID from the same client is treated as a new request.

---

## 4. Comparing Different Methods

### Money Storage: NUMERIC vs Float

Using `float` or `double` for money is a known source of bugs. For example, `0.1 + 0.2` in binary floating point is `0.30000000000000004`, not `0.3`. Over many operations this produces incorrect balances. `NUMERIC(19,4)` stores exact decimal values at the cost of slightly slower arithmetic — an acceptable trade-off for a payment system.

### Concurrency Control: Optimistic vs Pessimistic Locking

Optimistic locking adds a version column to each row. A transfer reads the version, does its calculation, and writes back only if the version has not changed. If another request modified the row in the meantime, the write fails and the caller retries. This works well when conflicts are rare but causes high retry rates under heavy concurrent load.

This system uses pessimistic locking (`SELECT FOR UPDATE`). Each transfer locks both wallets before reading balances. Other transfers that need the same wallets wait. This is correct and predictable under concurrent load and avoids the complexity of retry loops in application code. The ordered locking strategy (always lock lower UUID first) eliminates the deadlock risk that pessimistic locking normally carries.

### Test Database: Real PostgreSQL vs SQLite

SQLite is a common choice for integration tests because it is fast and has no server to manage. However, SQLite does not support `SELECT FOR UPDATE`, does not enforce `NUMERIC` precision the same way, and has different behaviour for several PostgreSQL constraints. Tests that pass against SQLite can fail against PostgreSQL in production.

All integration tests in this project run against a real PostgreSQL instance, either locally via Docker Compose or as a service container in the GitHub Actions CI pipeline. This means the test environment matches production closely.

### Balance Storage: Snapshot vs Replay

Two approaches exist for answering "what is the balance of wallet X at time T?":

1. **Replay:** Sum all ledger entries for wallet X up to time T. Correct, but slower as the ledger grows.
2. **Snapshot:** Store `balance_after` on each ledger entry. A single indexed query returns the balance at any point in time.

This system uses the snapshot approach. The `balance_after` column is written atomically as part of each transfer transaction, so it is always consistent with the ledger entries that precede it.

---

## 5. Testing and Data Results

### Test Categories

**Unit tests** cover domain entities and application services with all I/O mocked. A test verifying insufficient-funds behaviour creates a wallet with a known balance and asserts that a debit exceeding that balance throws `InsufficientFundsException`. No database is involved.

**Integration tests** use `WebApplicationFactory` to start the full application in memory against a real PostgreSQL test database. Each test class creates the database, runs migrations, seeds data, runs tests, then tears down. The full HTTP stack is exercised — routing, middleware, authentication, validation, and database writes.

**Concurrency tests** are the most critical. A wallet starts with a balance of 100.00. Ten parallel requests each attempt to transfer 20.00 from that wallet. Only five should succeed. The test asserts that exactly five requests return HTTP 201 and that the final wallet balance is exactly 0.00 — never negative.

### Coverage Targets

| Layer | Target |
|---|---|
| Domain entities | 95%+ |
| Application services | 85%+ |
| API endpoints (integration) | 80%+ |
| Concurrency scenarios | All critical paths covered |

### Idempotency Test Result

A duplicate idempotency key test sends the same transfer request twice with the same `Idempotency-Key` header. The first request returns HTTP 201 and debits the source wallet. The second request returns HTTP 200 with the original response body — the wallet is not debited a second time. The database contains exactly one ledger debit entry for the source wallet.

---

## 6. Access and Security

### Authentication

Users authenticate with email and password. Passwords are hashed with BCrypt before storage — plain-text passwords are never stored. On login, the server issues a JWT access token (15-minute expiry) and a refresh token (7-day expiry stored in the database).

Short access token expiry limits the damage if a token is leaked — it becomes useless after 15 minutes. The refresh token allows the client to get a new access token without requiring the user to log in again. On each refresh, the old refresh token is invalidated and a new one is issued. If a stolen refresh token is used after the legitimate client has already refreshed, the server detects reuse and can revoke the session.

### Role-Based Access Control

Three roles exist: `user`, `finance`, and `admin`. The role is embedded in the JWT as a claim and checked by ASP.NET Core policy-based authorization.

| Action | User | Finance | Admin |
|---|---|---|---|
| View and transfer own wallets | Yes | Yes | Yes |
| View any user's data | No | Yes | Yes |
| View full system ledger | No | Yes | Yes |
| Freeze or unfreeze wallets | No | No | Yes |

### Additional Security Measures

**Rate limiting** is enforced per user and per IP address to limit brute-force attacks and abuse. **FluentValidation** rejects malformed requests before they reach application logic. **UUID primary keys** prevent ID enumeration attacks — an attacker cannot guess the ID of another user's wallet by incrementing an integer. **CORS** is restricted to configured origins. Secrets (JWT signing key, database password) are stored in environment variables and GitHub Actions secrets — never committed to the repository.

---

## 7. Conclusion

The ledger system solves the real engineering problems of financial software: exact money arithmetic, atomic multi-table transactions, deadlock-safe concurrency control, idempotent payment operations, and auditable immutable records.

The key decisions — `NUMERIC(19,4)` for money, pessimistic locking in consistent UUID order, append-only ledger entries, and server-side idempotency deduplication — are each a direct response to a specific failure mode that a simpler approach would have. None of them are added complexity for its own sake.

The architecture keeps domain logic separate from infrastructure, which makes the core rules testable without a database and makes the system easier to change over time. The test suite validates correctness from unit level through to concurrent load scenarios against a real PostgreSQL instance.

The current design also has a clear upgrade path. The append-only ledger already exhibits the core properties of event sourcing. A future migration to full event sourcing — domain events, CQRS read models, and an outbox pattern for downstream subscribers — would not require destroying the existing schema. The foundation supports that evolution without a rewrite.
