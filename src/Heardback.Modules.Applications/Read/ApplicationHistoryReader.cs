using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Read;

/// <summary>Serves <see cref="IApplicationHistory"/>; the ownership filter keeps it to the caller's applications.</summary>
internal sealed class ApplicationHistoryReader(ApplicationsDbContext dbContext, NoResponseClock noResponse)
    : IApplicationHistory
{
    public async Task<IReadOnlyList<ApplicationHistory>> ListAsync(CancellationToken cancellationToken)
    {
        // One query with the status events as a collection; projection keeps notes and posting text out.
        var rows = await dbContext.Applications
            .AsNoTracking()
            .OrderByDescending(application => application.AppliedOn)
            .ThenByDescending(application => application.CreatedAt)
            .Select(application => new
            {
                application.Id,
                application.Company,
                application.Role,
                application.Channel,
                application.AppliedOn,
                application.LastActivityOn,
                application.Status,
                Changes = application.Events
                    .Where(timelineEvent => !timelineEvent.IsSubmission && timelineEvent.Status != null)
                    .OrderBy(timelineEvent => timelineEvent.OccurredOn)
                    .ThenBy(timelineEvent => timelineEvent.CreatedAt)
                    .Select(timelineEvent => new { Status = timelineEvent.Status!.Value, timelineEvent.OccurredOn })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ApplicationHistory(
                row.Id,
                row.Company,
                row.Role,
                row.Channel.ToString(),
                row.AppliedOn,
                row.LastActivityOn,
                noResponse.DaysFor(row.Status, row.LastActivityOn),
                row.Changes.Select(change => new ApplicationStatusChange(ApplicationStages.From(change.Status), change.OccurredOn)).ToList()))
            .ToList();
    }
}
