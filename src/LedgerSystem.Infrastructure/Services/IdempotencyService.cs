using LedgerSystem.Application.Interfaces;
using LedgerSystem.Application.Interfaces.Repositories;
using LedgerSystem.Application.Interfaces.Services;
using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Infrastructure.Services;

public sealed class IdempotencyService : IIdempotencyService
{
    private readonly IIdempotencyRepository _repository;
    private readonly IUnitOfWork _uow;

    public IdempotencyService(IIdempotencyRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<CachedResponse?> GetCachedResponseAsync(
        string key, Guid userId, CancellationToken ct = default)
    {
        var record = await _repository.FindAsync(key, userId, ct);

        if (record is null || record.IsExpired())
            return null;

        return new CachedResponse(record.ResponseStatus, "application/json", record.ResponseBody);
    }

    public async Task StoreResponseAsync(
        string key, Guid userId, string requestPath, CachedResponse response, CancellationToken ct = default)
    {
        var entry = IdempotencyKey.Create(
            key,
            userId,
            requestPath,
            response.StatusCode,
            response.Body);

        await _repository.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
