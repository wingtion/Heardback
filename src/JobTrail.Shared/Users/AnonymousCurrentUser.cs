namespace JobTrail.Shared.Users;

/// <summary>No user: sees no user-owned rows and cannot write any. Used at design time and outside requests.</summary>
public sealed class AnonymousCurrentUser : ICurrentUser
{
    public static readonly AnonymousCurrentUser Instance = new();

    private AnonymousCurrentUser()
    {
    }

    public Guid? UserId => null;

    public bool IsAdmin => false;
}
