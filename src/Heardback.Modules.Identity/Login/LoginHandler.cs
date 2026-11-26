using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Sessions;
using Heardback.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Login;

internal sealed partial class LoginHandler(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    SessionIssuer sessions,
    ILogger<LoginHandler> logger)
{
    public async Task<Result<IssuedSession>> HandleAsync(
        LoginRequest request,
        string? previousRefreshToken,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail && candidate.Kind == UserKind.Member,
            cancellationToken);

        if (user is null)
        {
            // Spend the same hashing time as a real check, so response timing does not reveal unknown emails.
            passwordHasher.VerifyHashedPassword(TimingDummy.User, TimingDummy.Hash, request.Password);
            LogFailed();
            return IdentityErrors.InvalidCredentials;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            LogFailed();
            return IdentityErrors.InvalidCredentials;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Saved together with the new session.
            user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        }

        LogSucceeded(user.Id);
        return await sessions.StartAsync(user, previousRefreshToken, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} signed in")]
    private partial void LogSucceeded(Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sign-in failed: invalid credentials")]
    private partial void LogFailed();

    private static class TimingDummy
    {
        public static readonly User User = User.Create("timing@invalid", "timing", isAdmin: false, DateTimeOffset.UnixEpoch);

        public static readonly string Hash = new PasswordHasher<User>().HashPassword(User, Guid.NewGuid().ToString());
    }
}
