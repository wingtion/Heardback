using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Recovery;
using Heardback.Modules.Identity.Sessions;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Account;

/// <summary>
/// Changes the signed-in user's password. Every session ends, other devices included, and this browser gets a new
/// one so the user stays signed in here (ADR-0015).
/// </summary>
internal sealed partial class ChangePasswordHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IPasswordHasher<User> passwordHasher,
    PasswordChanger passwordChanger,
    SessionIssuer sessionIssuer,
    ILogger<ChangePasswordHandler> logger)
{
    public async Task<Result<IssuedSession>> HandleAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (user.IsDemo)
        {
            return IdentityErrors.DemoAccountHasNoPassword;
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword)
            == PasswordVerificationResult.Failed)
        {
            return IdentityErrors.WrongCurrentPassword;
        }

        await passwordChanger.ChangeAsync(user, request.NewPassword, cancellationToken);
        var session = await sessionIssuer.StartAsync(user, previousRefreshToken: null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogChanged(userId);
        return session;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} changed their password")]
    private partial void LogChanged(Guid userId);
}
