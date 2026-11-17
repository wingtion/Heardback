using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using JobTrail.Shared.Persistence;

namespace JobTrail.Modules.Identity.Domain;

/// <summary>
/// One link in a rotation chain. Every refresh replaces the token with a new one in the same family;
/// presenting an already-replaced token revokes the whole family.
/// </summary>
[NotUserOwned("Looked up by token hash during refresh, before the caller is authenticated.")]
internal sealed class RefreshToken
{
    private const int TokenBytes = 32;

    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>All tokens descending from one login share a family.</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>SHA-256 of the token; the token itself is never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Set when this token was rotated; distinguishes rotation from logout.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public RefreshTokenState GetState(DateTimeOffset now, TimeSpan reuseGracePeriod)
    {
        if (RevokedAt is not { } revokedAt)
        {
            return now < ExpiresAt ? RefreshTokenState.Active : RefreshTokenState.Expired;
        }

        if (ReplacedByTokenId is null)
        {
            return RefreshTokenState.Revoked;
        }

        return now - revokedAt <= reuseGracePeriod ? RefreshTokenState.RecentlyRotated : RefreshTokenState.Reused;
    }

    public static IssuedRefreshToken Issue(Guid userId, Guid familyId, DateTimeOffset now, TimeSpan lifetime)
    {
        var plainToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        var token = new RefreshToken
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = Hash(plainToken),
            CreatedAt = now,
            ExpiresAt = now + lifetime,
        };

        return new IssuedRefreshToken(token, plainToken);
    }

    public static string Hash(string plainToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));
}

/// <summary>A newly issued token: the entity to store and the plain value to hand to the client once.</summary>
internal sealed record IssuedRefreshToken(RefreshToken Token, string PlainToken);
