using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Responses;
using Heardback.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Applications.Create;

internal sealed partial class CreateApplicationHandler(
    ApplicationsDbContext dbContext,
    PostingLinks postingLinks,
    NoResponseClock noResponse,
    TimeProvider timeProvider,
    ILogger<CreateApplicationHandler> logger)
{
    public async Task<Result<ApplicationResponse>> HandleAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var details = request.ToDetails();
        var posting = await postingLinks.ResolveUntrackedAsync(details, exceptApplicationId: null, cancellationToken);
        if (!posting.IsSuccess)
        {
            return posting.Error;
        }

        var created = Application.Create(details, posting.Value, timeProvider.GetUtcNow());
        if (!created.IsSuccess)
        {
            return created.Error;
        }

        var application = created.Value;
        dbContext.Applications.Add(application);
        var saved = await dbContext.SaveApplicationAsync(cancellationToken);
        if (!saved.IsSuccess)
        {
            return saved.Error;
        }

        LogCreated(application.Id, application.PostingId);
        return ApplicationResponse.From(application, noResponse);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application {ApplicationId} created for posting {PostingId}")]
    private partial void LogCreated(Guid applicationId, Guid postingId);
}
