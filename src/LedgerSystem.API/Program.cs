// LedgerSystem.API entry point
// Populated in M3 (Authentication) and beyond.
// Each milestone adds to this file incrementally.

var builder = WebApplication.CreateBuilder(args);

// M3: builder.Services.AddAuthentication(...)
// M3: builder.Services.AddAuthorization(...)
// M3: builder.Services.AddScoped<ITokenService, JwtTokenService>()
// M4: builder.Services.AddScoped<IWalletRepository, WalletRepository>()
// M5: builder.Services.AddScoped<ITransferService, TransferService>()
// M6: builder.Services.AddRateLimiter(...)
// M7: builder.Services.AddAuthorization(options => { ... })
// M9: builder.Host.UseSerilog(...)

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// M3: builder.Services.AddSwaggerGen(c => { ... })

var app = builder.Build();

// M6: app.UseMiddleware<IdempotencyMiddleware>()
// M6: app.UseMiddleware<GlobalExceptionHandlerMiddleware>()

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check — available from M1
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Required for WebApplicationFactory in integration tests (M8)
public partial class Program { }
