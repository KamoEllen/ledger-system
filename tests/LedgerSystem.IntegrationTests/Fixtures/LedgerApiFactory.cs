using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LedgerSystem.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory that replaces PostgreSQL with a shared SQLite in-memory database
/// and swaps out the PostgreSQL-specific WalletRepository for a SQLite-compatible version.
///
/// A single <see cref="SqliteConnection"/> is kept open for the lifetime of the factory
/// so that the EF Core in-memory schema is not dropped between requests.
///
/// Usage in xUnit test classes:
/// <code>
/// public class MyTests : IClassFixture&lt;LedgerApiFactory&gt;
/// {
///     private readonly HttpClient _client;
///     public MyTests(LedgerApiFactory factory) => _client = factory.CreateClient();
/// }
/// </code>
/// </summary>
public sealed class LedgerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // One connection shared across all EF Core operations for this factory instance.
    private readonly SqliteConnection _connection =
        new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // ── 1. Remove the real DbContext registration ─────────────────────
            services.RemoveAll<DbContextOptions<LedgerDbContext>>();
            services.RemoveAll<LedgerDbContext>();

            // ── 2. Register SQLite in-memory DbContext ────────────────────────
            services.AddDbContext<LedgerDbContext>(options =>
                options.UseSqlite(_connection));

            // ── 3. Swap out the PostgreSQL-specific WalletRepository ──────────
            services.RemoveAll<IWalletRepository>();
            services.AddScoped<IWalletRepository, TestWalletRepository>();

            // ── 4. Remove background services (avoid timer noise in tests) ────
            services.RemoveAll<IHostedService>();
        });
    }

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        // Open connection once — keeps the SQLite in-memory DB alive.
        await _connection.OpenAsync();

        // Create the schema (runs OnModelCreating, applies configurations).
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    // ── Helpers for tests ─────────────────────────────────────────────────────

    /// <summary>
    /// Resets all tables to a clean state between test classes that share
    /// the same factory instance. Call in IAsyncLifetime.InitializeAsync
    /// if per-test isolation is needed.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();

        db.IdempotencyKeys.RemoveRange(db.IdempotencyKeys);
        db.LedgerEntries.RemoveRange(db.LedgerEntries);
        db.Transfers.RemoveRange(db.Transfers);
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        db.Wallets.RemoveRange(db.Wallets);
        db.Users.RemoveRange(db.Users);

        await db.SaveChangesAsync();
    }
}
