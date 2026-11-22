namespace Heardback.Shared.Privacy;

/// <summary>
/// Removes everything one module keeps about a user when they delete their account (ADR-0015). Every module that
/// stores user data implements it; the Identity module runs them all and deletes the user last.
/// </summary>
public interface IUserDataEraser
{
    /// <summary>
    /// Deletes the module's data for <paramref name="userId"/>. Runs in the deleting user's own request, so the
    /// ownership filters apply. Must be safe to run again after a partial failure.
    /// </summary>
    /// <param name="userId">The signed-in user whose account is being deleted.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>A task that completes when the data is gone.</returns>
    Task EraseAsync(Guid userId, CancellationToken cancellationToken);
}
