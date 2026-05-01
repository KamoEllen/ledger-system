namespace LedgerSystem.Application.Settings;

public sealed class AuthSettings
{
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
