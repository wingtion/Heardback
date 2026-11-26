namespace Heardback.Modules.Identity.Authentication;

/// <summary>Claim names and values in Heardback access tokens. Inbound claim mapping is off, so these arrive unchanged.</summary>
internal static class IdentityClaims
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string AdminRole = "admin";

    /// <summary>Present only on demo visitors' tokens, so their limits apply without a database lookup.</summary>
    public const string AccountKind = "account";
    public const string DemoAccount = "demo";
}
