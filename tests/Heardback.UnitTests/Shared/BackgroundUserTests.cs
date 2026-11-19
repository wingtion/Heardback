using Heardback.Shared.Users;

namespace Heardback.UnitTests.Shared;

public sealed class BackgroundUserTests
{
    [Fact]
    public void Set_SameUserTwice_KeepsTheUser()
    {
        var userId = Guid.NewGuid();
        var user = new BackgroundUser();

        user.Set(userId);
        user.Set(userId);

        Assert.Equal(userId, user.UserId);
    }

    [Fact]
    public void Set_DifferentUser_Throws()
    {
        var user = new BackgroundUser();
        user.Set(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => user.Set(Guid.NewGuid()));
    }

    [Fact]
    public void UserId_NeverSet_IsNull()
    {
        Assert.Null(new BackgroundUser().UserId);
    }
}
