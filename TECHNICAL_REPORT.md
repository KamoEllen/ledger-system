# Technical Report: Ledger System

**Stack:** .NET 8 / ASP.NET Core, PostgreSQL 16, Angular 17  
**Domain:** Financial Technology — Double-Entry Ledger and Payment API  
**Date:** April 2026

---

## Table of Contents

1. [Summary](#1-summary) — A factual overview of the ledger system, the tools used (.NET 8, PostgreSQL), and the specific problems it solves regarding money movement and data accuracy.

2. [Purpose and Requirements](#2-purpose-and-requirements)
   - 2.1 [The Need for Double-Entry Accounting](#21-the-need-for-double-entry-accounting) — Why this specific method is required for financial accuracy and audit trails.
   - 2.2 [System Goals](#22-system-goals) — The mandatory rules the system follows, such as preventing double-spending and keeping a permanent record of every change.

3. [System Design and Choices](#3-system-design-and-choices)
   - 3.1 [Data Structure](#31-data-structure) — Why the system uses NUMERIC(19,4) for money and UUIDs for IDs to ensure data is exact and secure.
   - 3.2 [Organizing the Code](#32-organizing-the-code) — Using a layered structure to keep business rules separate from the database and the API.
   - 3.3 [Handling Simultaneous Requests](#33-handling-simultaneous-requests) — How the system uses a specific locking order based on UUID to prevent the database from getting stuck when two people send money at the same time.

4. [Comparing Different Methods](#4-comparing-different-methods)
   - 4.1 [Storing Balances vs. Storing History](#41-storing-balances-vs-storing-history) — A comparison between keeping only the current balance and keeping a full history of transactions, and why this system does both.
   - 4.2 [Handling Network Failures (Idempotency)](#42-handling-network-failures-idempotency) — How the system uses a unique key for each request so that if a user clicks "send" twice by mistake, the money is only moved once.
   - 4.3 [Test Environments](#43-test-environments) — Why the system uses a real PostgreSQL database for testing instead of a simplified one like SQLite.

5. [Testing and Data Results](#5-testing-and-data-results)
   - 5.1 [Concurrency Results](#51-concurrency-results) — Figures showing that when 10 requests are made at once for the same money, only the correct number succeed and the balance stays at zero.
   - 5.2 [Test Coverage](#52-test-coverage) — A breakdown of how much of the code is covered by automated tests.

6. [Access and Security](#6-access-and-security)
   - 6.1 [User Roles](#61-user-roles) — Plain definitions of what a User, Finance Officer, and Admin can and cannot see.
   - 6.2 [Login Security](#62-login-security) — How passwords are protected and how login tokens expire to keep the system safe.

7. [Conclusion](#7-conclusion) — Final notes on the system's ability to maintain a correct financial record under heavy use.

---

## 1. Summary

This report covers the design and implementation of a double-entry ledger payment system built on .NET 8 and PostgreSQL 16. The system lets users hold currency wallets and transfer money between them. Every transfer is recorded as two immutable ledger entries — a debit on the sender's side and a credit on the receiver's side — so the books always balance and a permanent audit trail is always available.

The system is built to handle the core problems of financial software: preventing double-spending when multiple requests arrive at the same time, making the API safe to retry on network failure without charging twice, and enforcing strict rules about who can see or change financial data. It is structured in four code layers, containerised with Docker, and deployed automatically through a GitHub Actions pipeline.

---

## 2. Purpose and Requirements

### 2.1 The Need for Double-Entry Accounting

In single-entry bookkeeping, a transfer subtracts an amount from one account and adds it to another. If the system crashes between those two steps, money disappears. Double-entry bookkeeping solves this by treating every financial event as two sides of the same record. A debit of $150 from wallet A and a credit of $150 to wallet B are written together in a single atomic operation. Either both are written or neither is. Money cannot appear or vanish.

This method also produces a complete, permanent history of every movement. Each ledger entry is written once and never changed. If an error occurs, a new correcting entry is added — the original stays in the record. This is the standard used by banks and payment processors because it makes fraud and mistakes easier to detect.

### 2.2 System Goals

**Mandatory rules the system must follow:**

- No transfer can reduce a wallet balance below zero, even when dozens of requests arrive simultaneously
- Every balance change — deposit, debit, credit — must be recorded as a permanent ledger entry that cannot be edited or deleted
- A transfer that fails halfway must leave no trace: both wallet balances and both ledger entries must roll back together
- A client that sends the same transfer request twice (due to a network failure) must not be charged twice
- Only authenticated users can access any data; elevated roles are required for financial administration

---

## 3. System Design and Choices

![Ledger System Architecture](https://github.com/KamoEllen/ledger-system/blob/main/System-Architecture-Diagram-CleanLayered-Architecture.svg)
         
### 3.1 Data Structure

All money columns in the database use the `NUMERIC(19,4)` data type. This stores exact decimal values. To understand why this matters, consider what happens with the more common `float` or `double` types.

In binary floating point, the number `0.10` cannot be represented exactly. The closest possible value is `0.1000000000000000055511151231257827021181583404541015625`. When you add `0.10` and `0.20` using floating-point arithmetic, the result is `0.30000000000000004`, not `0.30`. A single transaction carries an error of `0.000000000000000004`. Over ten million transactions, that error compounds into a real discrepancy in the books.

`NUMERIC(19,4)` stores the number as an exact decimal. `0.10 + 0.20` equals `0.30`, without exception. The trade-off is slightly slower arithmetic, which is acceptable for a payment system where correctness is required.

All primary keys use UUID v4 instead of sequential integers. An integer ID is predictable: if a user's wallet ID is 1042, they can guess that wallet 1043 belongs to someone else and attempt to access it. A UUID like `a3f8c2d1-4b7e-41c9-b2f0-9e6d3a1c8f05` cannot be guessed. UUIDs also work correctly when the system runs on multiple servers at the same time, since each server can generate IDs independently without coordinating with the others.

### 3.2 Organizing the Code

The codebase is split into four separate projects with a strict rule about which layer can depend on which:

```
API → Application → Domain ← Infrastructure
```

The **Domain** layer contains the business rules: entities like `Wallet` and `Transfer`, value objects like `Money`, and domain exceptions like `InsufficientFundsException`. This layer has no knowledge of databases, HTTP, or any external library. Its rules can be tested with plain code and no setup.

The **Application** layer contains the use-case logic — the steps for executing a transfer, for example. It defines interfaces (contracts) for what it needs from the database but does not implement them.

The **Infrastructure** layer implements those interfaces using Entity Framework Core and PostgreSQL. If the database is swapped in the future, only this layer changes.

The **API** layer handles HTTP: routing, request validation, authentication middleware, and mapping requests to application commands.

This separation means the financial rules in the Domain layer are never mixed with database queries or HTTP details. A rule like "a wallet cannot go below zero" lives in one place and is tested independently.

### 3.3 Handling Simultaneous Requests

![Ledger System Architecture](https://github.com/KamoEllen/ledger-system/blob/main/Sequence-Diagram-(Deadlock-Prevention).svg)
         
A deadlock is a situation where two operations are each waiting for the other to finish, so neither can proceed. Without a specific precaution, this system would be vulnerable to deadlocks during concurrent transfers.

Consider this scenario: User A sends $50 to User B, and at the same moment User B sends $50 to User A.

- Request 1 locks wallet A to read its balance, then tries to lock wallet B.
- Request 2 locks wallet B to read its balance, then tries to lock wallet A.
- Request 1 is waiting for wallet B, which Request 2 holds. Request 2 is waiting for wallet A, which Request 1 holds. Neither can move forward. The database is stuck.

The fix is simple: always lock wallets in the same order, regardless of which direction the money is moving. This system sorts the two wallet IDs alphabetically before locking them. Because UUIDs are strings, they have a natural alphabetical order.

In the scenario above, both Request 1 and Request 2 sort the two IDs and lock the one that comes first alphabetically. Request 1 gets there first, locks it, then locks the second. Request 2 tries to lock the first ID, finds it already locked, and waits. When Request 1 finishes and releases both locks, Request 2 proceeds. They queue instead of deadlock. No crash, no data loss.

---

## 4. Comparing Different Methods

### 4.1 Storing Balances vs. Storing History

Two approaches exist for knowing the current balance of a wallet:

**Replay:** Store only the history of transactions. To find the current balance, sum all credits and subtract all debits for that wallet. This is perfectly accurate but gets slower as the number of transactions grows. A wallet with ten years of history requires summing thousands of entries every time someone checks their balance.

**Snapshot:** Store the current balance directly on the wallet row, and update it on every transaction. A balance lookup is instant. However, if the snapshot and the ledger ever become inconsistent — due to a bug or a failed transaction — the stored balance is wrong with no way to detect it.

This system uses both. The wallet row holds the current balance for fast lookups. Each ledger entry also stores a `balance_after` field — the exact wallet balance at the moment that entry was written. This is done inside the same transaction as the balance update, so they are always consistent. A point-in-time balance query uses a single indexed lookup on `balance_after` rather than replaying all prior history.

### 4.2 Handling Network Failures (Idempotency)

Networks are unreliable. A client sends a transfer request, the server processes it and debits the wallet, but the response never arrives because the connection dropped. The client, seeing no response, sends the request again. Without a safeguard, the wallet is debited twice.

The fix is idempotency. The client generates a unique identifier (a UUID) before sending the request and includes it in an `Idempotency-Key` header. The server stores this key along with the full response it returned. If the same key arrives again within 24 hours, the server returns the stored response immediately — it does not re-execute the transfer. The client gets its answer, and the wallet is debited only once.

This is the same mechanism used by Stripe, PayPal, and most other payment APIs. It shifts the responsibility for generating the unique key to the client, which is the right place for it since the client is the one who knows whether a request is a retry.

### 4.3 Test Environments


![Ledger System Architecture](https://github.com/KamoEllen/ledger-system/blob/main/CICD-Pipeline-Flow.svg)
  
SQLite is often used for integration tests because it runs in memory, requires no server, and is fast. For many applications it is a reasonable choice. For this system it is not, because of three specific gaps:

**Row-level locking.** The transfer logic relies on `SELECT FOR UPDATE` to lock wallet rows while a transaction is in progress. SQLite does not have row-level locking. It locks the entire database file. A test that passes against SQLite tells you nothing about whether the row-level locking logic works correctly.

**Exact decimal types.** PostgreSQL's `NUMERIC` type stores exact decimal values and enforces the specified precision. SQLite uses "numeric affinity," which maps decimal values to floating-point storage internally. A test that passes with correct decimal arithmetic in SQLite may hide a precision error that only appears against PostgreSQL in production.

**Constraint enforcement.** PostgreSQL enforces `CHECK` constraints, unique indexes, and foreign key integrity in specific ways that differ from SQLite. For example, the `balance_non_negative` check (`CHECK (balance >= 0)`) behaves correctly in PostgreSQL under concurrent writes. SQLite's handling of the same constraint under concurrent access is different.

All integration tests in this project run against a real PostgreSQL 16 instance — locally via Docker Compose, and in the CI pipeline via a PostgreSQL service container in GitHub Actions.

---

## 5. Testing and Data Results

### 5.1 Concurrency Results

The concurrency test is the most direct proof that the locking logic works. The setup:

- One source wallet with a starting balance of **$100.00**
- Ten parallel HTTP requests, each attempting to transfer **$20.00** from that wallet
- Only five transfers can succeed before the wallet reaches zero

**Result:** Exactly five requests return HTTP 201 (transfer completed). The remaining five return HTTP 422 with error code `INSUFFICIENT_FUNDS`. The final wallet balance is **$0.00** — not negative, not $20.00, exactly zero. No overdraft occurred across any of the ten concurrent attempts.

The idempotency test sends the same transfer request twice using the same `Idempotency-Key`. The first request returns HTTP 201 and writes one debit entry to the ledger. The second request returns HTTP 200 with the original response body. The database contains exactly one debit entry. The wallet balance reflects one deduction, not two.

### 5.2 Test Coverage

| Layer | Target Coverage |
|---|---|
| Domain entities | 95%+ |
| Application services | 85%+ |
| API endpoints (integration) | 80%+ |
| Concurrency scenarios | All critical paths |

Unit tests cover domain logic with all database and HTTP calls mocked. Integration tests start the full application against a real database and exercise every endpoint through its full request lifecycle — middleware, authentication, validation, database write, and response. Coverage reports are generated using the `dotnet-reportgenerator` tool after each test run.

---

## 6. Access and Security

### 6.1 User Roles

Three roles exist, each with a defined scope of access:

| Action | User | Finance Officer | Admin |
|---|---|---|---|
| View own wallets and balance | Yes | Yes | Yes |
| Initiate transfers from own wallets | Yes | Yes | Yes |
| View own transaction history | Yes | Yes | Yes |
| View any user's wallets and history | No | Yes | Yes |
| View the full system ledger | No | Yes | Yes |
| Freeze or unfreeze any wallet | No | No | Yes |
| Manage user accounts | No | No | Yes |

A regular user can only see and move their own money. A Finance Officer can read data across all accounts for auditing purposes but cannot change anything. An Admin has full write access including the ability to freeze a wallet, which blocks all outgoing transfers from it.

### 6.2 Login Security

Passwords are hashed with BCrypt before being stored. BCrypt is a slow hashing function by design — it takes a measurable amount of time to compute. This makes it expensive to run bulk password-guessing attacks against the stored hashes. The plain-text password is never stored anywhere.

On login, the server issues two tokens. The **access token** is a JWT that expires after 15 minutes. It is included in every API request as a header. If this token is stolen, it stops working after 15 minutes without any action required. The **refresh token** expires after 7 days and is stored in the database. When the access token expires, the client sends the refresh token to get a new one. The server immediately invalidates the old refresh token and issues a new one. If a stolen refresh token is used after the real client has already refreshed, the server detects the reuse and can revoke the entire session.

Rate limiting is applied per user and per IP address. This prevents automated scripts from making thousands of requests in a short time. All secrets — the JWT signing key and the database password — are stored in environment variables and GitHub Actions secrets. They are never written into the codebase.

---

## 7. Conclusion

The ledger system addresses the specific failure modes of financial software: rounding errors from imprecise number types, lost updates from non-atomic transactions, deadlocks from unordered concurrent access, duplicate charges from network retries, and data loss from mutable records.

Each design decision maps directly to one of those problems. `NUMERIC(19,4)` exists because float arithmetic produces wrong answers. Ordered wallet locking exists because unordered locking causes deadlocks. Idempotency keys exist because networks drop responses. Append-only ledger entries exist because financial records must not change after the fact.

The layered architecture keeps the financial rules isolated from the infrastructure, which means the rules can be tested and verified without a database. The test suite confirms correctness at three levels: domain logic in isolation, full HTTP endpoints against a real database, and concurrent load against a shared wallet.

The system's append-only ledger is already structured as an event log, which makes a future move to full event sourcing straightforward. The schema supports that transition without a destructive migration — the foundation is in place without over-engineering the first version.
