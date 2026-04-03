using LedgerSystem.Domain.Enums;

namespace LedgerSystem.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core navigation property — not exposed as a collection to mutate
    private readonly List<Wallet> _wallets = new();
    public IReadOnlyList<Wallet> Wallets => _wallets.AsReadOnly();

    // Required by EF Core
    private User() { Email = string.Empty; PasswordHash = string.Empty; }

    private User(Guid id, string email, string passwordHash, UserRole role, DateTime now)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static User Create(string email, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        return new User(Guid.NewGuid(), email.Trim().ToLowerInvariant(), passwordHash, role, DateTime.UtcNow);
    }

    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePasswordHash(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newHash));

        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }
}
