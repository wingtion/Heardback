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
        // Domain checks come first, so a refused request never writes to the shared posting catalogue.
        var created = Application.Create(request.ToDetails(), timeProvider.GetUtcNow());
        if (!created.IsSuccess)
        {
            return created.Error;
        }

        var application = created.Value;
        var linked = await postingLinks.LinkAsync(application, cancellationToken);
        if (!linked.IsSuccess)
        {
            return linked.Error;
        }

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
