using Heardback.Modules.Applications.Create;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Responses;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Update;

internal sealed class UpdateApplicationHandler(
    ApplicationsDbContext dbContext,
    PostingLinks postingLinks,
    NoResponseClock noResponse,
    TimeProvider timeProvider)
{
    public async Task<Result<ApplicationResponse>> HandleAsync(
        Guid id,
        UpdateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .Include(candidate => candidate.Events)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (application is null)
        {
            return ApplicationsErrors.NotFound;
        }

        if (application.Version != request.Version)
        {
            return ApplicationsErrors.ChangedElsewhere;
        }

        // Compare against the version the form was opened with when saving, so an edit that lands between
        // this read and the save is detected too.
        dbContext.Entry(application).Property(candidate => candidate.Version).OriginalValue = request.Version!.Value;

        var previousKeys = (application.NormalizedUrl, application.ContentHash);
        var edited = application.Edit(request.ToDetails(), request.Notes, timeProvider.GetUtcNow());
        if (!edited.IsSuccess)
        {
            return edited.Error;
        }

        // The shared posting is never edited (ADR-0004). When the keys that identify the posting change, the
        // application is linked to whichever posting matches now; otherwise it keeps its posting.
        if (previousKeys != (application.NormalizedUrl, application.ContentHash))
        {
            var linked = await postingLinks.LinkAsync(application, cancellationToken);
            if (!linked.IsSuccess)
            {
                return linked.Error;
            }
        }

        var saved = await dbContext.SaveApplicationAsync(cancellationToken);
        return saved.IsSuccess ? ApplicationResponse.From(application, noResponse) : saved.Error;
    }
}
