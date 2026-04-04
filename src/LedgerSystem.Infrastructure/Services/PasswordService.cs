using LedgerSystem.Application.Interfaces.Services;

namespace LedgerSystem.Infrastructure.Services;

public sealed class PasswordService : IPasswordService
{
    // Work factor 12 is the production recommendation (≈250ms per hash on modern hardware).
    // Development seeder uses 4 for speed — do not use 4 in production.
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);

    public bool Verify(string plainTextPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainTextPassword, hash);
}
