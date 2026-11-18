using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Heardback.Modules.Applications.Persistence;

internal static class DuplicatePostingSave
{
    private const string UserPostingIndex = "ix_applications_user_id_posting_id";

    /// <summary>
    /// Saves, or returns false when the user already tracks the posting: two submits racing past the
    /// "already tracked" check are stopped by the unique index.
    /// </summary>
    public static async Task<bool> TrySaveWithoutDuplicatePostingAsync(
        this ApplicationsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UserPostingIndex,
        })
        {
            return false;
        }
    }
}
