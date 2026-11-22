using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Recovery;

/// <summary>Issues a reset code for a user, replacing any code of theirs that is still open (ADR-0015).</summary>
internal sealed class ResetCodeIssuer(IdentityDbContext dbContext, TimeProvider timeProvider)
{
    /// <param name="userId">Whose password the code resets.</param>
    /// <param name="issuedByUserId">The admin issuing it, or null for the owner command.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The code as shown to people (the only copy) and when it expires.</returns>
    public async Task<ResetCodeResponse> IssueAsync(Guid userId, Guid? issuedByUserId, CancellationToken cancellationToken)
    {
        var (code, plainCode) = PasswordResetCode.Issue(userId, issuedByUserId, timeProvider.GetUtcNow());

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.PasswordResetCodes
            .Where(open => open.UserId == userId && open.UsedAt == null)
            .ExecuteDeleteAsync(cancellationToken);
        dbContext.PasswordResetCodes.Add(code);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ResetCodeResponse(plainCode, code.ExpiresAt);
    }
}
