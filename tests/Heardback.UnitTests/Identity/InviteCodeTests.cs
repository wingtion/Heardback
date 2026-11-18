using Heardback.Modules.Identity.Domain;

namespace Heardback.UnitTests.Identity;

public sealed class InviteCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    [Fact]
    public void Generate_NewCode_HasTwelveUnambiguousCharacters()
    {
        var invite = InviteCode.Generate(Guid.CreateVersion7(), Now, Lifetime);

        Assert.Equal(InviteCode.CodeLength, invite.Code.Length);
        Assert.DoesNotContain(invite.Code, character => "ILOU".Contains(character, StringComparison.Ordinal));
    }

    [Fact]
    public void DisplayCode_NormalizedCode_IsGroupedInFours()
    {
        Assert.Equal("K7QX-M2PA-9TDF", InviteCode.Format("K7QXM2PA9TDF"));
    }

    [Theory]
    [InlineData("k7qx-m2pa-9tdf")]
    [InlineData("  K7QX M2PA 9TDF ")]
    [InlineData("K7QXM2PA9TDF")]
    public void Normalize_TypedVariants_ReturnCanonicalCode(string typed)
    {
        Assert.Equal("K7QXM2PA9TDF", InviteCode.Normalize(typed));
    }

    [Fact]
    public void GetStatus_BeforeExpiry_IsActive()
    {
        var invite = InviteCode.Generate(Guid.CreateVersion7(), Now, Lifetime);

        Assert.Equal(InviteCodeStatus.Active, invite.GetStatus(Now + Lifetime - TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void GetStatus_AtExpiry_IsExpired()
    {
        var invite = InviteCode.Generate(Guid.CreateVersion7(), Now, Lifetime);

        Assert.Equal(InviteCodeStatus.Expired, invite.GetStatus(Now + Lifetime));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void IsActiveAt_AroundExpiry_AgreesWithGetStatus(int secondsFromExpiry)
    {
        var invite = InviteCode.Generate(Guid.CreateVersion7(), Now, Lifetime);
        var at = Now + Lifetime + TimeSpan.FromSeconds(secondsFromExpiry);

        var isActive = InviteCode.IsActiveAt(at).Compile()(invite);

        Assert.Equal(invite.GetStatus(at) == InviteCodeStatus.Active, isActive);
    }
}
