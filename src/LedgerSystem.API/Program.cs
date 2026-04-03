// LedgerSystem.API entry point
// Each milestone adds to this file incrementally.
// M2: Infrastructure (DbContext, repositories, seeder)
// M3: Authentication + Swagger
// M4: Wallet controllers
// M5: Transfer service
// M6: Idempotency middleware + global error handler + rate limiting
// M7: RBAC policies + admin controllers
// M9: Serilog structured logging

using LedgerSystem.Infrastructure;
using LedgerSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── M2: Infrastructure ────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── M3: builder.Services.AddAuthentication(...) ───────────────────────────
// ── M3: builder.Services.AddAuthorization(...) ────────────────────────────
// ── M3: builder.Services.AddSwaggerGen(c => { ... }) ─────────────────────
// ── M5: builder.Services.AddScoped<ITransferService, TransferService>() ───
// ── M6: builder.Services.AddRateLimiter(...) ──────────────────────────────
// ── M9: builder.Host.UseSerilog(...) ──────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ── Run database seed when --seed flag is passed ──────────────────────────
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
    return;
}

// ── M6: app.UseMiddleware<IdempotencyMiddleware>() ────────────────────────
// ── M6: app.UseMiddleware<GlobalExceptionHandlerMiddleware>() ─────────────

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint — verifies DB connectivity
app.MapGet("/health", async (LedgerDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    catch
    {
        return Results.Json(
            new { status = "unhealthy", timestamp = DateTime.UtcNow },
            statusCode: 503);
    }
});

app.Run();

// Required for WebApplicationFactory in integration tests (M8)
public partial class Program { }
