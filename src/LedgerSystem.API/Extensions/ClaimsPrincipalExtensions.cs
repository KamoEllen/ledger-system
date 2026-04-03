using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LedgerSystem.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the user ID from the JWT sub claim.
    /// Throws if the claim is missing — only call this on [Authorize] endpoints.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException(
                "JWT sub claim is missing or not a valid GUID. " +
                "Ensure [Authorize] is applied before calling GetUserId().");

        return userId;
    }
}
