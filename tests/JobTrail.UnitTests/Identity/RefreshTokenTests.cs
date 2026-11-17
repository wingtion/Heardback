using System.Reflection;
using JobTrail.Modules.Identity.Domain;

namespace JobTrail.UnitTests.Identity;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(30);

    [Fact]
    public void Issue_NewToken_StoresHashNotPlainValue()
    {
        var issued = RefreshToken.Issue(Guid.CreateVersion7(), Guid.CreateVersion7(), Now, Lifetime);

        Assert.NotEqual(issued.PlainToken, issued.Token.TokenHash);
        Assert.Equal(RefreshToken.Hash(issued.PlainToken), issued.Token.TokenHash);
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
        RefreshToken.Issue(Guid.CreateVersion7(), Guid.CreateVersion7(), Now, Lifetime).Token;

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
