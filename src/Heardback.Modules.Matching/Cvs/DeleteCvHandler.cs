using Heardback.Modules.Matching.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Matching.Cvs;

internal sealed partial class DeleteCvHandler(MatchingDbContext dbContext, ILogger<DeleteCvHandler> logger)
{
    /// <summary>
    /// Removes the user's CV for good, with the tailoring suggestions made from it. Deleting when there is none
    /// also succeeds, so a retry after a lost response is harmless. The ownership filter limits both statements to
    /// the user's own rows.
    /// </summary>
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var deleted = await dbContext.Cvs.ExecuteDeleteAsync(cancellationToken);
        await dbContext.TailoringResults.ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (deleted > 0)
        {
            LogDeleted();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "CV deleted")]
    private partial void LogDeleted();
}
