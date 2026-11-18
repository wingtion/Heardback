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

        var details = request.ToDetails();
        var postingId = await ResolvePostingAsync(application, details, cancellationToken);
        if (!postingId.IsSuccess)
        {
            return postingId.Error;
        }

        var edited = application.Edit(details, request.Notes, postingId.Value, timeProvider.GetUtcNow());
        if (!edited.IsSuccess)
        {
            return edited.Error;
        }

        var saved = await dbContext.SaveApplicationAsync(cancellationToken);
        return saved.IsSuccess ? ApplicationResponse.From(application, noResponse) : saved.Error;
    }

    // The shared posting is never edited (ADR-0004). When what identifies the posting changes, the application
    // is linked to whichever posting matches now; otherwise it keeps its posting.
    private async Task<Result<Guid>> ResolvePostingAsync(
        Application application,
        ApplicationDetails details,
        CancellationToken cancellationToken)
    {
        var identifiesSamePosting = SameText(application.Url, details.Url)
            && SameText(application.PostingText, details.PostingText)
            && SameText(application.Company, details.Company)
            && SameText(application.Role, details.Role);

        return identifiesSamePosting
            ? application.PostingId
            : await postingLinks.ResolveUntrackedAsync(details, application.Id, cancellationToken);
    }

    private static bool SameText(string? stored, string? submitted) =>
        string.Equals(
            string.IsNullOrWhiteSpace(stored) ? null : stored.Trim(),
            string.IsNullOrWhiteSpace(submitted) ? null : submitted.Trim(),
            StringComparison.Ordinal);
}
