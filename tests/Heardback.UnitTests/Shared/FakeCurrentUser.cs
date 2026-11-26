using Heardback.Shared.Users;

namespace Heardback.UnitTests.Shared;

internal sealed class FakeCurrentUser(bool isDemo) : ICurrentUser
{
    public Guid? UserId { get; } = Guid.NewGuid();

    public bool IsAdmin => false;

    public bool IsDemo { get; } = isDemo;
}
