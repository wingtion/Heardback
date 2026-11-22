using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Applications.Delete;

internal sealed partial class DeleteApplicationHandler(
    ApplicationsDbContext dbContext,
    IEnumerable<IApplicationDeletedHandler> deletedHandlers,
    UnusedPostingCleanup postingCleanup,
    ILogger<DeleteApplicationHandler> logger)
{
    /// <summary>
    /// Removes the application and its timeline for good, and lets other modules remove what they keep about it.
    /// The shared posting goes too when no other application uses it (ADR-0015).
    /// </summary>
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var posting = await dbContext.Applications.AsNoTracking()
            .Where(application => application.Id == id)
            .Select(application => new { application.PostingId, application.ContentHash })
            .FirstOrDefaultAsync(cancellationToken);
        if (posting is null)
        {
            return ApplicationsErrors.NotFound;
        }

        // One DELETE by id, so a double click or a delete racing a board move cannot hit a row version
        // conflict: whoever comes second simply finds nothing. The ownership filter applies to this statement,
        // and the database removes the timeline through its cascading foreign key.
        var deleted = await dbContext.Applications
            .Where(application => application.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return ApplicationsErrors.NotFound;
        }

        foreach (var handler in deletedHandlers)
        {
            await handler.OnDeletedAsync(id, cancellationToken);
        }

        await postingCleanup.RemoveUnusedAsync(
            [posting.PostingId],
            posting.ContentHash is null ? [] : [posting.ContentHash],
            cancellationToken);

        LogDeleted(id);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application {ApplicationId} deleted")]
    private partial void LogDeleted(Guid applicationId);
}
