using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Account;

/// <summary>
/// Deletes the signed-in user's account and everything personal with it (ADR-0015), after checking the password and
/// that the last admin stays.
/// </summary>
internal sealed partial class DeleteAccountHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IPasswordHasher<User> passwordHasher,
    AccountRemover remover,
    ILogger<DeleteAccountHandler> logger)
{
    public async Task<Result> HandleAsync(DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        // Locks the admins (and this user) until the end, so two admins deleting at once cannot both pass the
        // "not the last admin" check.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlAsync(
            $"SELECT id FROM identity.users WHERE is_admin OR id = {userId} FOR UPDATE",
            cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            return IdentityErrors.WrongPassword;
        }

        if (user.IsAdmin && await dbContext.Users.CountAsync(candidate => candidate.IsAdmin, cancellationToken) == 1)
        {
            return IdentityErrors.LastAdmin;
        }

        await remover.RemoveAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogDeleted(userId);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} deleted their account")]
    private partial void LogDeleted(Guid userId);
}
