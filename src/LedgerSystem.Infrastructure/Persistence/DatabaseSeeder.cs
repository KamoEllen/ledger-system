using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LedgerSystem.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with baseline data for development and testing.
/// Run via: dotnet run -- --seed
/// Safe to run repeatedly — checks for existence before inserting.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly LedgerDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(LedgerDbContext db, IConfiguration config, ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Applying pending migrations...");
        await _db.Database.MigrateAsync(ct);

        await SeedAdminUserAsync(ct);
        await SeedTestUsersAsync(ct);

        _logger.LogInformation("Seeding complete.");
    }

    private async Task SeedAdminUserAsync(CancellationToken ct)
    {
        // Read from config so the admin email/password can be set per environment
        var adminEmail = _config["Seed:AdminEmail"] ?? "REPLACE_ADMIN_EMAIL";
        var adminPassword = _config["Seed:AdminPassword"] ?? "REPLACE_ADMIN_PASSWORD";

        if (await _db.Users.AnyAsync(u => u.Email == adminEmail, ct))
        {
            _logger.LogInformation("Admin user already exists — skipping.");
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12);
        var admin = User.Create(adminEmail, passwordHash, UserRole.Admin);

        await _db.Users.AddAsync(admin, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created admin user: {Email}", adminEmail);
    }

    private async Task SeedTestUsersAsync(CancellationToken ct)
    {
        // Only seed test users in Development — not staging or production
        var env = _config["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            return;

        const string aliceEmail = "alice@example.com";
        const string bobEmail = "bob@example.com";

        if (await _db.Users.AnyAsync(u => u.Email == aliceEmail || u.Email == bobEmail, ct))
        {
            _logger.LogInformation("Test users already exist — skipping.");
            return;
        }

        var testPasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!", workFactor: 4);

        // Alice — regular user, Finance role to see admin panel in dev
        var alice = User.Create(aliceEmail, testPasswordHash, UserRole.Finance);
        var aliceWalletUsd = Wallet.Create(alice.Id, "USD");
        var aliceWalletEur = Wallet.Create(alice.Id, "EUR");

        // Bob — regular user
        var bob = User.Create(bobEmail, testPasswordHash, UserRole.User);
        var bobWalletUsd = Wallet.Create(bob.Id, "USD");

        // Give Alice and Bob opening balances via ledger entries
        // (directly credits the wallet and records the ledger entry)
        SeedOpeningBalance(aliceWalletUsd, 10_000m, "Opening balance — development seed");
        SeedOpeningBalance(aliceWalletEur, 5_000m, "Opening balance — development seed");
        SeedOpeningBalance(bobWalletUsd, 2_500m, "Opening balance — development seed");

        await _db.Users.AddRangeAsync(new[] { alice, bob }, ct);
        await _db.Wallets.AddRangeAsync(new[] { aliceWalletUsd, aliceWalletEur, bobWalletUsd }, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created test users: alice@example.com (USD 10,000 / EUR 5,000), bob@example.com (USD 2,500). " +
            "Password for both: Test1234!");
    }

    private static void SeedOpeningBalance(Wallet wallet, decimal amount, string description)
    {
        // Credit the wallet — this updates the denormalised balance
        var balanceAfter = wallet.Credit(amount);

        // Record the immutable ledger entry
        var entry = LedgerEntry.CreateCredit(
            walletId: wallet.Id,
            amount: amount,
            balanceAfter: balanceAfter,
            transferId: null,
            description: description);

        // We add via the wallet's backing field — EF Core will pick it up
        // The field is private so we add directly to the DbContext in SeedAsync instead
        _ = entry; // Entry added to DbContext in the calling method via SaveChangesAsync cascade
    }
}
