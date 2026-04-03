using LedgerSystem.Domain.Entities;

namespace LedgerSystem.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();

    /// <summary>
    /// Validates an access token and returns the user ID it contains.
    /// Returns null if the token is invalid or expired.
    /// </summary>
    Guid? GetUserIdFromToken(string accessToken);
}
