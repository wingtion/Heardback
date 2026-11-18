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
    /// <summary>The current user's application for the posting behind this link, or null.</summary>
    public async Task<TrackedApplicationResponse?> HandleAsync(string url, CancellationToken cancellationToken)
    {
        if (await postings.FindIdByUrlAsync(url, cancellationToken) is not { } postingId)
        {
            return null;
        }

        return await dbContext.Applications
            .AsNoTracking()
            .Where(application => application.PostingId == postingId)
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
