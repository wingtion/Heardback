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

    /// <summary>When each posting was first submitted by anyone, for the "listed a long time" hint (ADR-0013).</summary>
    /// <param name="postingIds">The postings to look up.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The first-seen time per posting id; unknown ids are left out.</returns>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> FirstSeenAsync(
        IReadOnlyCollection<Guid> postingIds,
        CancellationToken cancellationToken);
}
