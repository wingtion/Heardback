using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Postings.Contracts;
using Heardback.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Delete;

/// <summary>
/// After applications are deleted or moved to another posting, deletes the shared postings and posting analyses no
/// application uses any more, so a posting's text never outlives the last application that uses it (ADR-0015).
/// </summary>
internal sealed class UnusedPostingCleanup(ApplicationsDbContext dbContext, IJobPostings postings)
{
    /// <summary>Removes whichever of the given postings and analyses no application of any user uses.</summary>
    /// <param name="postingIds">Postings the removed applications were linked to.</param>
    /// <param name="contentHashes">Content hashes of the removed applications' posting text.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    public async Task RemoveUnusedAsync(
        IReadOnlyCollection<Guid> postingIds,
        IReadOnlyCollection<string> contentHashes,
        CancellationToken cancellationToken)
    {
        foreach (var postingId in postingIds.Distinct())
        {
            await RemovePostingIfUnusedAsync(postingId, cancellationToken);
        }

        if (contentHashes.Count > 0)
        {
            await RemoveUnusedAnalysesAsync(contentHashes, cancellationToken);
        }
    }

    // The lock makes "nobody uses it" and the delete one step for anyone linking to the posting at the same time:
    // a link either committed before the check (and is seen) or waits and then finds the posting gone.
    private async Task RemovePostingIfUnusedAsync(Guid postingId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockPostingAsync(postingId, cancellationToken);

        var used = await dbContext.Applications
            .IgnoreQueryFilters([ModuleDbContext.UserOwnershipFilter])
            .AnyAsync(application => application.PostingId == postingId, cancellationToken);
        if (!used)
        {
            await postings.DeleteAsync(postingId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    // An analysis is shared by every application with the same text (ADR-0008). Without a lock: an application added
    // with that text a moment later simply asks for a new analysis.
    private Task<int> RemoveUnusedAnalysesAsync(
        IReadOnlyCollection<string> contentHashes,
        CancellationToken cancellationToken)
    {
        var applications = dbContext.Applications.IgnoreQueryFilters([ModuleDbContext.UserOwnershipFilter]);
        return dbContext.PostingAnalyses
            .Where(analysis => contentHashes.Contains(analysis.ContentHash)
                && !applications.Any(application => application.ContentHash == analysis.ContentHash))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
