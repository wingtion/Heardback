using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Heardback.Modules.Applications.Persistence;

internal static class DuplicatePostingSave
{
    private const string UserPostingIndex = "ix_applications_user_id_posting_id";

    /// <summary>
    /// Saves, turning the two expected write conflicts into errors: the user already tracks the posting (two
    /// submits racing past the "already tracked" check hit the unique index), or the application's row version
    /// changed since it was read (an edit elsewhere).
    /// </summary>
    public static async Task<Result> SaveApplicationAsync(
        this ApplicationsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApplicationsErrors.ChangedElsewhere;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UserPostingIndex,
        })
        {
            return ApplicationsErrors.AlreadyTracked;
        }
    }
}
