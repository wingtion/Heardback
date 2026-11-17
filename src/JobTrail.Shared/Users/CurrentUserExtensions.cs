namespace JobTrail.Shared.Users;

public static class CurrentUserExtensions
{
    /// <summary>
    /// The authenticated user's id. Only call this behind authorization; an anonymous caller here is a bug.
    /// </summary>
    public static Guid GetRequiredUserId(this ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        return currentUser.UserId
            ?? throw new InvalidOperationException("No authenticated user in the current context.");
    }
}
