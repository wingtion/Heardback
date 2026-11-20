using Heardback.Infrastructure.Llm.Persistence;
using Heardback.Shared.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Infrastructure.Llm.Account;

/// <summary>
/// Deletes the analysis jobs a user requested and their daily usage counts (ADR-0015). The app-wide call count holds
/// no user id and stays. A job already running simply finds its row gone when it finishes.
/// </summary>
internal sealed class LlmDataEraser(LlmDbContext dbContext) : IUserDataEraser
{
    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.AnalysisJobs
            .Where(job => job.RequestedByUserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.DailyUsage
            .Where(usage => usage.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
