namespace Heardback.Shared.Users;

/// <summary>
/// The user making the current request. The id always comes from the validated access token,
/// never from the request body or route.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id, or <c>null</c> for anonymous requests.</summary>
    Guid? UserId { get; }

    bool IsAdmin { get; }

    /// <summary>A "Try the demo" visitor (ADR-0018): smaller AI quotas, and community signals only from demo data.</summary>
    bool IsDemo { get; }
}
