using Heardback.Modules.Matching.Persistence;
using Heardback.Shared.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Account;

/// <summary>Deletes a user's CV text and skills, AI consent and tailoring suggestions (ADR-0015).</summary>
internal sealed class MatchingDataEraser(MatchingDbContext dbContext) : IUserDataEraser
{
    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        // The ownership filter limits every statement to the signed-in user, whose id this is.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.TailoringResults.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiConsents.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Cvs.ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
