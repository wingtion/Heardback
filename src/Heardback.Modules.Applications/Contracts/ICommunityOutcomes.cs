namespace Heardback.Modules.Applications.Contracts;

/// <summary>
/// The only read across users (ADR-0013): what other members' applications show for the postings and companies
/// the signed-in user tracks. It returns counts, never rows, ids or text from other users. Groups with fewer than
/// the minimum number of other members, and every group the user does not track, are left out entirely.
/// </summary>
public interface ICommunityOutcomes
{
    /// <summary>The signed-in user's own applications with the community groups they belong to.</summary>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The snapshot; its group lists hold only groups that passed the minimum size.</returns>
    Task<CommunitySnapshot> ForCurrentUserAsync(CancellationToken cancellationToken);
}
