using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Responses;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Applications.Timeline;

internal sealed partial class AddEventHandler(
    ApplicationsDbContext dbContext,
    NoResponseClock noResponse,
    TimeProvider timeProvider,
    ILogger<AddEventHandler> logger)
{
    // Status is computed from the whole timeline, so two updates saved at once must not both win with a
    // timeline that misses the other's event. The row version makes the later save fail; it then re-reads
    // the timeline and tries again.
    private const int MaxAttempts = 3;

    public async Task<Result<ApplicationResponse>> HandleAsync(
        Guid id,
        AddEventRequest request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var application = await dbContext.Applications
                .Include(candidate => candidate.Events)
                .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (application is null)
            {
                return ApplicationsErrors.NotFound;
            }

            var added = application.AddEvent(
                request.Status, request.OccurredOn!.Value, request.Note, timeProvider.GetUtcNow());
            if (!added.IsSuccess)
            {
                return added.Error;
            }

            var saved = await dbContext.SaveApplicationAsync(cancellationToken);
            if (saved.IsSuccess)
            {
                LogEventAdded(application.Id, added.Value.Status);
                return ApplicationResponse.From(application, noResponse);
            }

            if (saved.Error != ApplicationsErrors.ChangedElsewhere)
            {
                return saved.Error;
            }

            dbContext.ChangeTracker.Clear();
        }

        return ApplicationsErrors.ChangedElsewhere;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application {ApplicationId} got an update (status {Status})")]
    private partial void LogEventAdded(Guid applicationId, ApplicationStatus? status);
}
