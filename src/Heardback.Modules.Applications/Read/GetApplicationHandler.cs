using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Responses;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Read;

internal sealed class GetApplicationHandler(ApplicationsDbContext dbContext, NoResponseClock noResponse)
{
    public async Task<Result<ApplicationResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        // Another user's application is hidden by the ownership filter, so it is simply "not found".
        var application = await dbContext.Applications
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new ApplicationResponse(
                candidate.Id,
                candidate.Company,
                candidate.Role,
                candidate.Url,
                candidate.PostingText,
                candidate.Channel,
                candidate.AppliedOn,
                candidate.Status,
                noResponse.DaysFor(candidate.Status, candidate.LastActivityOn),
                candidate.Notes,
                candidate.UpdatedAt,
                candidate.Version,
                candidate.Events
                    .OrderByDescending(timelineEvent => timelineEvent.OccurredOn)
                    .ThenByDescending(timelineEvent => timelineEvent.CreatedAt)
                    .Select(timelineEvent => new ApplicationEventResponse(
                        timelineEvent.Id,
                        timelineEvent.Status,
                        timelineEvent.OccurredOn,
                        timelineEvent.Note,
                        timelineEvent.IsSubmission))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return application is null ? ApplicationsErrors.NotFound : application;
    }
}
