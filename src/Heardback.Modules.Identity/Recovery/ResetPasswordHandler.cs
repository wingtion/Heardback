using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Recovery;

/// <summary>Sets a new password with a reset code from an admin or the owner (ADR-0015).</summary>
internal sealed partial class ResetPasswordHandler(
    IdentityDbContext dbContext,
    PasswordChanger passwordChanger,
    TimeProvider timeProvider,
    ILogger<ResetPasswordHandler> logger)
{
    public async Task<Result> HandleAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        // One answer for every failure, so the response never tells whether a code exists.
        if (!PasswordResetCode.IsWellFormed(request.Code))
        {
            return IdentityErrors.InvalidResetCode;
        }

        var hash = PasswordResetCode.Hash(request.Code);
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var open = dbContext.PasswordResetCodes
            .Where(code => code.CodeHash == hash && code.UsedAt == null && code.ExpiresAt > now);
        var userId = await open.Select(code => (Guid?)code.UserId).FirstOrDefaultAsync(cancellationToken);

        // The conditional update is the claim: two requests with the same code wait on the row, and the second
        // finds it used.
        if (userId is null
            || await open.ExecuteUpdateAsync(setters => setters.SetProperty(code => code.UsedAt, now), cancellationToken) == 0)
        {
            return IdentityErrors.InvalidResetCode;
        }

        var user = await dbContext.Users.FirstAsync(candidate => candidate.Id == userId, cancellationToken);
        await passwordChanger.ChangeAsync(user, request.NewPassword, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogReset(user.Id);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} set a new password with a reset code")]
    private partial void LogReset(Guid userId);
}
