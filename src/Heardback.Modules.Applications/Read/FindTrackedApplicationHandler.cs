using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Responses;
using Heardback.Modules.Postings.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Read;

internal sealed class FindTrackedApplicationHandler(
    ApplicationsDbContext dbContext,
    IJobPostings postings,
    NoResponseClock noResponse)
{
    /// <summary>
    /// The current user's application for this link: one with the same link key, or one linked to the posting
    /// that has this link (the same check adding the link would make). Both queries always run, so the response
    /// time does not tell whether some other user has submitted the link.
    /// </summary>
    public async Task<TrackedApplicationResponse?> HandleAsync(string url, CancellationToken cancellationToken)
    {
        var normalizedUrl = PostingKeys.NormalizeUrl(url);
        var postingId = await postings.FindIdByUrlAsync(url, cancellationToken);

        return await dbContext.Applications
            .AsNoTracking()
            .Where(application => application.NormalizedUrl == normalizedUrl
                || (postingId != null && application.PostingId == postingId))
            .Select(application => new TrackedApplicationResponse(
                application.Id,
                application.Company,
                application.Role,
                application.Status,
                noResponse.DaysFor(application.Status, application.LastActivityOn),
                application.AppliedOn))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
