using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Account;

/// <summary>
/// Removes an account and everything personal with it (ADR-0015): every module's eraser first, then sessions,
/// invite codes and the user row. Safe to retry after a failure, because the user row goes last. The current user
/// of the scope must be the account being removed, so the modules' ownership filters apply to the erasers.
/// </summary>
internal sealed class AccountRemover(IdentityDbContext dbContext, IEnumerable<IUserDataEraser> erasers)
{
    public async Task RemoveAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var eraser in erasers)
        {
            await eraser.EraseAsync(userId, cancellationToken);
        }

        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // Invite codes only record who invited whom; keeping one would keep the removed user's id.
        await dbContext.InviteCodes
            .Where(invite => invite.CreatedByUserId == userId || invite.UsedByUserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Users
            .Where(candidate => candidate.Id == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
