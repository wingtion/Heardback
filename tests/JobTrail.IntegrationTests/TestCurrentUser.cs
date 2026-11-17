using JobTrail.Shared.Users;

namespace JobTrail.IntegrationTests;

internal sealed class TestCurrentUser(Guid? userId) : ICurrentUser
{
    public Guid? UserId { get; } = userId;

    public bool IsAdmin => false;
}
