namespace Heardback.Modules.Applications.Contracts;

/// <summary>
/// Read access to the posting details the signed-in user keeps on their own applications (ADR-0004: each
/// application has the user's own copy). Other users' applications are never visible through it.
/// </summary>
public interface IApplicationPostings
{
    /// <summary>The posting details of one of the signed-in user's applications.</summary>
    /// <param name="applicationId">The application's id.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>
    /// The details, or <see langword="null"/> when the application does not exist, was deleted, or belongs to
    /// another user.
    /// </returns>
    Task<ApplicationPosting?> FindAsync(Guid applicationId, CancellationToken cancellationToken);
}
