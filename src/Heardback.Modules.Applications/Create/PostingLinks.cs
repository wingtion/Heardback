using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Postings.Contracts;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Create;

/// <summary>Links an application to its shared posting, refusing a posting the user already tracks.</summary>
internal sealed class PostingLinks(ApplicationsDbContext dbContext, IJobPostings postings)
{
    // A resolved posting is only gone again when its last application was deleted in that same moment.
    private const int MaxResolveAttempts = 3;

    /// <summary>
    /// Checks the user's own applications by link key and content hash first, so a duplicate never touches the
    /// shared catalogue. Only then finds or creates the posting, and checks the posting id too: a submission can
    /// match a posting through another user's link or text. Call after the domain checks have passed, inside the
    /// transaction that saves the application (ADR-0015).
    /// </summary>
    public async Task<Result> LinkAsync(Application application, CancellationToken cancellationToken)
    {
        if (await TracksSameKeysAsync(application, cancellationToken))
        {
            return ApplicationsErrors.AlreadyTracked;
        }

        for (var attempt = 1; attempt <= MaxResolveAttempts; attempt++)
        {
            var posting = await postings.ResolveAsync(
                new PostingSubmission(application.Url, application.Company, application.Role, application.PostingText),
                cancellationToken);
            if (!posting.IsSuccess)
            {
                return posting.Error;
            }

            // Held until the caller's transaction ends, so the posting cannot be deleted as unused before the
            // application is saved. If it was deleted between resolving and locking, resolve again.
            await dbContext.LockPostingAsync(posting.Value, cancellationToken);
            if (!await postings.ExistsAsync(posting.Value, cancellationToken))
            {
                continue;
            }

            if (await TracksPostingAsync(application, posting.Value, cancellationToken))
            {
                return ApplicationsErrors.AlreadyTracked;
            }

            application.LinkTo(posting.Value);
            return Result.Success();
        }

        throw new InvalidOperationException("The resolved job posting kept being deleted before it could be linked.");
    }

    // The ownership filter limits both checks to the current user's applications.
    private Task<bool> TracksSameKeysAsync(Application application, CancellationToken cancellationToken) =>
        dbContext.Applications.AnyAsync(
            other => other.Id != application.Id
                && ((application.NormalizedUrl != null && other.NormalizedUrl == application.NormalizedUrl)
                    || (application.ContentHash != null && other.ContentHash == application.ContentHash)),
            cancellationToken);

    private Task<bool> TracksPostingAsync(Application application, Guid postingId, CancellationToken cancellationToken) =>
        dbContext.Applications.AnyAsync(
            other => other.Id != application.Id && other.PostingId == postingId,
            cancellationToken);
}
