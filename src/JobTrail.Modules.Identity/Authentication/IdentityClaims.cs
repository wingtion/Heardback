namespace JobTrail.Modules.Identity.Authentication;

/// <summary>Claim names and values in JobTrail access tokens. Inbound claim mapping is off, so these arrive unchanged.</summary>
internal static class IdentityClaims
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string AdminRole = "admin";
}
