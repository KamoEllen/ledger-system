namespace LedgerSystem.Application;

/// <summary>
/// Named authorization policy constants used by controllers and registered in Program.cs.
/// Backed by the ClaimTypes.Role claim set from UserRole enum in the JWT.
/// </summary>
public static class Policies
{
    /// <summary>Accessible by Admin role only.</summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>Accessible by Finance or Admin roles.</summary>
    public const string RequireFinanceOrAdmin = "RequireFinanceOrAdmin";
}
