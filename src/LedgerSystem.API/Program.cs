// LedgerSystem.API — entry point
// M2: Infrastructure (DbContext, repositories)
// M3: JWT Authentication + Swagger + FluentValidation
// M4: Wallet controllers
// M5: Transfer service
// M6: Idempotency middleware + global error handler + rate limiting + Serilog  ← current milestone
// M7: RBAC policies + admin controllers

using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using LedgerSystem.API.Middleware;
using LedgerSystem.Application.Validators;
using LedgerSystem.Infrastructure;
using LedgerSystem.Infrastructure.Persistence;
using LedgerSystem.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// ── Serilog bootstrap logger ──────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Ledger System API...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LedgerSystem")
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/ledger-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30));

    // ── Infrastructure (DbContext, repositories, services) ────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── JWT Authentication ────────────────────────────────────────────────────
    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()!;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero   // No grace period — token expiry is exact
            };
        });

    builder.Services.AddAuthorization();

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    // Per-user (JWT sub claim) sliding window; falls back to IP for anonymous.
    var rateLimitOptions = builder.Configuration.GetSection("RateLimit");
    var permitLimit = rateLimitOptions.GetValue("PermitLimit", 100);
    var windowSeconds = rateLimitOptions.GetValue("WindowSeconds", 60);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddSlidingWindowLimiter("per-user", limiterOptions =>
        {
            limiterOptions.PermitLimit = permitLimit;
            limiterOptions.Window = TimeSpan.FromSeconds(windowSeconds);
            limiterOptions.SegmentsPerWindow = 6;
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        // Key resolver: use JWT sub claim when present, otherwise client IP
        options.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await ctx.HttpContext.Response.WriteAsJsonAsync(
                new { code = "RATE_LIMIT_EXCEEDED", message = "Too many requests. Please slow down." }, ct);
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var partitionKey = string.IsNullOrEmpty(userId)
                ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                : userId;

            return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });
    });

    // ── FluentValidation ──────────────────────────────────────────────────────
    builder.Services
        .AddFluentValidationAutoValidation()
        .AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

    // ── Controllers + Swagger ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Ledger System API",
            Version = "v1",
            Description = "Double-entry ledger payment API"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token: Bearer {token}"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // ── Run seeder when --seed flag is passed ─────────────────────────────────
    if (args.Contains("--seed"))
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        return;
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
    // Order matters: exception handler must be first so it catches all downstream errors.
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ledger System API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms";
    });

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // Idempotency middleware runs after auth so the user identity is resolved.
    app.UseMiddleware<IdempotencyMiddleware>();

    app.MapControllers();

    // Health check — verifies DB connectivity
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
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory in integration tests (M8)
public partial class Program { }
