# LedgerSystem.IntegrationTests

Integration tests are added in **M8** using `WebApplicationFactory<Program>`.

They spin up the full application in memory against a real PostgreSQL test database
and exercise the full HTTP stack — routing, middleware, auth, and database.

## What gets tested in M8

- All API endpoints (auth, wallets, transfers, admin)
- Idempotency — duplicate requests return the same response
- Concurrency — 10 parallel transfers against one wallet, assert no overdraft
- RBAC — requests with wrong roles return 403
