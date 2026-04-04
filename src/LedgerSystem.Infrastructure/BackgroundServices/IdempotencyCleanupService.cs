using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LedgerSystem.Infrastructure.BackgroundServices;

/// <summary>
/// Runs hourly and deletes idempotency keys whose ExpiresAt has passed.
/// Uses a dedicated DI scope so it gets a fresh DbContext per run.
/// </summary>
public sealed class IdempotencyCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdempotencyCleanupService> _logger;

    public IdempotencyCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<IdempotencyCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Idempotency cleanup service started.");

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during idempotency key cleanup.");
            }
        }

        _logger.LogInformation("Idempotency cleanup service stopped.");
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIdempotencyRepository>();

        await repository.DeleteExpiredAsync(ct);
        _logger.LogInformation("Idempotency key cleanup completed at {Time}.", DateTime.UtcNow);
    }
}
