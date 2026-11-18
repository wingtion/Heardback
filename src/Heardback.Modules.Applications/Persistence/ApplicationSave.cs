using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Heardback.Modules.Applications.Persistence;

internal static class ApplicationSave
{
    private const string UserPostingIndex = "ix_applications_user_id_posting_id";
    private const string EventApplicationForeignKey = "fk_application_events_applications_application_id";

    /// <summary>
    /// Saves, turning the expected write conflicts into errors: the user already tracks the posting (two
    /// submits racing past the "already tracked" check hit the unique index), the application's row version
    /// changed since it was read (an edit elsewhere), or the application was deleted while a timeline event
    /// was being added (the event's foreign key fails).
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
        catch (DbUpdateException exception) when (ToExpectedError(exception) is { } error)
        {
            return error;
        }
    }

    private static Error? ToExpectedError(DbUpdateException exception) =>
        exception.InnerException switch
        {
            PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: UserPostingIndex } =>
                ApplicationsErrors.AlreadyTracked,
            PostgresException
            {
                SqlState: PostgresErrorCodes.ForeignKeyViolation,
                ConstraintName: EventApplicationForeignKey,
            } => ApplicationsErrors.NotFound,
            _ => null,
        };
}
