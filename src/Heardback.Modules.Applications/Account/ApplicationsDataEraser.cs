using Heardback.Modules.Applications.Delete;
using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Account;

/// <summary>
/// Deletes a user's applications with their timeline and notes, then the shared postings and posting analyses no
/// one else uses (ADR-0015). Other modules remove what they derived from the applications through their own eraser.
/// </summary>
internal sealed class ApplicationsDataEraser(ApplicationsDbContext dbContext, UnusedPostingCleanup postingCleanup)
    : IUserDataEraser
{
    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        // The ownership filter limits both statements to the signed-in user, whose id this is.
        var linked = await dbContext.Applications.AsNoTracking()
            .Select(application => new { application.PostingId, application.ContentHash })
            .ToListAsync(cancellationToken);

        await dbContext.Applications.ExecuteDeleteAsync(cancellationToken);

        await postingCleanup.RemoveUnusedAsync(
            [.. linked.Select(application => application.PostingId)],
            [.. linked.Select(application => application.ContentHash).OfType<string>().Distinct()],
            cancellationToken);
    }
}
