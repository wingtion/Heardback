using Heardback.Shared.Users;

namespace Heardback.IntegrationTests;

internal sealed class TestCurrentUser(Guid? userId) : ICurrentUser
{
    public Guid? UserId { get; } = userId;

    public bool IsAdmin => false;
}
