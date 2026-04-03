# LedgerSystem.Infrastructure

Populated in **M2** with:
- `Persistence/LedgerDbContext.cs` — EF Core DbContext
- `Persistence/Configurations/` — Fluent API entity configurations
- `Persistence/Migrations/` — EF Core migrations
- `Repositories/` — Implementations of all `IRepository` interfaces from Application layer

Dependencies added in M2:
- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore.Design`
