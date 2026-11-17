using System.Reflection;
using Heardback.Modules.Identity.Domain;

namespace Heardback.UnitTests.Identity;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan Absolute = TimeSpan.FromDays(60);
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(30);

    [Fact]
    public void Issue_NewToken_StoresHashNotPlainValue()
    {
        var issued = RefreshToken.StartSession(Guid.CreateVersion7(), Now, Lifetime, Absolute);

        Assert.NotEqual(issued.PlainToken, issued.Token.TokenHash);
        Assert.Equal(RefreshToken.Hash(issued.PlainToken), issued.Token.TokenHash);
    }

    [Fact]
    public void StartSession_NewSession_ExpiresAfterSlidingLifetimeWithAbsoluteLimitFixed()
    {
        var token = Issue();

        Assert.Equal(Now + Lifetime, token.ExpiresAt);
        Assert.Equal(Now + Absolute, token.SessionExpiresAt);
    }

    [Fact]
    public void Rotate_WellWithinSession_SlidesExpiryAndKeepsFamilyAndSessionEnd()
    {
        var token = Issue();
        var later = Now + TimeSpan.FromDays(10);

        var next = token.Rotate(later, Lifetime).Token;

        Assert.Equal(later + Lifetime, next.ExpiresAt);
        Assert.Equal(token.FamilyId, next.FamilyId);
        Assert.Equal(token.SessionExpiresAt, next.SessionExpiresAt);
    }

    [Fact]
    public void Rotate_CloseToAbsoluteLimit_CapsExpiryAtSessionEnd()
    {
        var token = Issue();

        var next = token.Rotate(Now + TimeSpan.FromDays(55), Lifetime).Token;

        Assert.Equal(Now + Absolute, next.ExpiresAt);
    }

    [Fact]
    public void GetState_BeforeExpiry_IsActive()
    {
        var token = Issue();

        Assert.Equal(RefreshTokenState.Active, token.GetState(Now + Lifetime - TimeSpan.FromSeconds(1), Grace));
    }

    [Fact]
    public void GetState_AtExpiry_IsExpired()
    {
        var token = Issue();

        Assert.Equal(RefreshTokenState.Expired, token.GetState(Now + Lifetime, Grace));
    }

    [Fact]
    public void GetState_RevokedWithoutReplacement_IsRevoked()
    {
        var token = Issue();
        Set(token, nameof(RefreshToken.RevokedAt), Now);

        Assert.Equal(RefreshTokenState.Revoked, token.GetState(Now, Grace));
    }

    [Fact]
    public void GetState_RotatedWithinGracePeriod_IsRecentlyRotated()
    {
        var token = Rotated();

        Assert.Equal(RefreshTokenState.RecentlyRotated, token.GetState(Now + Grace, Grace));
    }

    [Fact]
    public void GetState_RotatedAfterGracePeriod_IsReused()
    {
        var token = Rotated();

        Assert.Equal(RefreshTokenState.Reused, token.GetState(Now + Grace + TimeSpan.FromSeconds(1), Grace));
    }

    private static RefreshToken Issue() =>
        RefreshToken.StartSession(Guid.CreateVersion7(), Now, Lifetime, Absolute).Token;

    private static RefreshToken Rotated()
    {
        var token = Issue();
        Set(token, nameof(RefreshToken.RevokedAt), Now);
        Set(token, nameof(RefreshToken.ReplacedByTokenId), Guid.CreateVersion7());
        return token;
    }

    // Rotation happens through a database UPDATE, so tests set the persisted state directly.
    private static void Set(RefreshToken token, string property, object value) =>
        typeof(RefreshToken).GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!.SetValue(token, value);
}
