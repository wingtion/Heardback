using Heardback.Shared.Results;

namespace Heardback.Modules.Postings.Contracts;

/// <summary>
/// The shared catalogue of job postings. A posting is shared by every user who tracks it and is never changed
/// by one of them; it exists to recognise the same job across users (ADR-0004).
/// </summary>
public interface IJobPostings
{
    /// <summary>
    /// Returns the id of the posting that matches the submission, creating it when none does. Matches by
    /// normalized link first, then by company, role and posting text. Safe to call concurrently: the same job
    /// submitted at the same time still ends up as one posting.
    /// </summary>
    /// <param name="submission">What the user entered.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The posting id, or a validation error when the link is not a valid http(s) link.</returns>
    Task<Result<Guid>> ResolveAsync(PostingSubmission submission, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the posting a link belongs to without creating anything, e.g. to tell a user they already track it.
    /// </summary>
    /// <param name="url">The link as the user entered it.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The posting id, or <see langword="null"/> when the link is invalid or no posting has it.</returns>
    Task<Guid?> FindIdByUrlAsync(string url, CancellationToken cancellationToken);

    /// <summary>Whether the posting still exists; it is deleted once no application uses it (ADR-0015).</summary>
    /// <param name="postingId">The posting to look for.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns><see langword="true"/> when the posting exists.</returns>
    Task<bool> ExistsAsync(Guid postingId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a posting. The caller decides that no application uses it any more and guards that decision against
    /// a concurrent link (ADR-0015). Deleting a posting that is already gone succeeds.
    /// </summary>
    /// <param name="postingId">The posting to delete.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>A task that completes when the posting is gone.</returns>
    Task DeleteAsync(Guid postingId, CancellationToken cancellationToken);

    /// <summary>When each posting was first submitted by anyone, for the "listed a long time" hint (ADR-0013).</summary>
    /// <param name="postingIds">The postings to look up.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The first-seen time per posting id; unknown ids are left out.</returns>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> FirstSeenAsync(
        IReadOnlyCollection<Guid> postingIds,
        CancellationToken cancellationToken);
}
