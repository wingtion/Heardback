namespace Heardback.Shared.Authorization;

/// <summary>Named authorization policies shared across modules. The Identity module registers them.</summary>
public static class AuthorizationPolicies
{
    public const string Admin = "Admin";
}
