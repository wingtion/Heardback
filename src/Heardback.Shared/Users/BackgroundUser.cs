namespace Heardback.Shared.Users;

/// <summary>
/// The user a background job runs for, in that job's own service scope. The job queue sets it from the job row,
/// whose user id was taken from the access token when the user asked for the work, so per-user filters keep
/// applying to background work exactly as they do to the request.
/// </summary>
public sealed class BackgroundUser
{
    public Guid? UserId { get; private set; }

    /// <summary>Sets the user once per scope; a scope never switches users.</summary>
    public void Set(Guid userId)
    {
        if (UserId is { } current && current != userId)
        {
            throw new InvalidOperationException("A scope cannot run for two users.");
        }

        UserId = userId;
    }
}
