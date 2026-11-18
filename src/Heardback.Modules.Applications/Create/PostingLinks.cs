using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Postings.Contracts;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Create;

/// <summary>Finds the shared posting for the user's details and refuses one the user already tracks.</summary>
internal sealed class PostingLinks(ApplicationsDbContext dbContext, IJobPostings postings)
{
    /// <param name="details">What the user entered.</param>
    /// <param name="exceptApplicationId">The application being edited, which may keep its own posting.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task<Result<Guid>> ResolveUntrackedAsync(
        ApplicationDetails details,
        Guid? exceptApplicationId,
        CancellationToken cancellationToken)
    {
        var posting = await postings.ResolveAsync(
            new PostingSubmission(details.Url, details.Company, details.Role, details.PostingText),
            cancellationToken);
        if (!posting.IsSuccess)
        {
            return posting.Error;
        }

        // The ownership filter limits this to the current user's applications.
        var alreadyTracked = await dbContext.Applications.AnyAsync(
            application => application.PostingId == posting.Value && application.Id != exceptApplicationId,
            cancellationToken);

        return alreadyTracked ? ApplicationsErrors.AlreadyTracked : posting.Value;
    }
}
