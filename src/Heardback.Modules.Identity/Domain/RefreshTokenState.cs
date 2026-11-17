namespace Heardback.Modules.Identity.Domain;

internal enum RefreshTokenState
{
    Active,
    Expired,

    /// <summary>Revoked by logout or family revocation.</summary>
    Revoked,

    /// <summary>Replaced moments ago, e.g. a second tab refreshing at the same time. Rejected, but not treated as theft.</summary>
    RecentlyRotated,

    /// <summary>Replaced a while ago and presented again: the token was likely copied, so the family is revoked.</summary>
    Reused,
}
