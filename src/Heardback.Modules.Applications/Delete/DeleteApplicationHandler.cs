using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Applications.Delete;

internal sealed partial class DeleteApplicationHandler(
    ApplicationsDbContext dbContext,
    ILogger<DeleteApplicationHandler> logger)
{
    /// <summary>Removes the application and its timeline for good. The shared posting stays.</summary>
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (application is null)
        {
            return ApplicationsErrors.NotFound;
        }

        dbContext.Applications.Remove(application);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogDeleted(id);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application {ApplicationId} deleted")]
    private partial void LogDeleted(Guid applicationId);
}
