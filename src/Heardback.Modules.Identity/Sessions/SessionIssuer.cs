using Heardback.Modules.Identity.Authentication;
using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Identity.Sessions;

/// <summary>
/// Starts a signed-in session: a new refresh-token family (with its absolute expiry fixed now) plus a fresh
/// access token.
/// </summary>
internal sealed partial class SessionIssuer(
    IdentityDbContext dbContext,
    AccessTokenIssuer accessTokens,
    RefreshTokenRevoker revoker,
    IOptions<TokenSettings> settings,
    TimeProvider timeProvider,
    ILogger<SessionIssuer> logger)
{
    /// <param name="user">The user signing in.</param>
    /// <param name="previousRefreshToken">
    /// The refresh cookie this browser still sent, if any. The new session replaces that cookie, so the
    /// session it belonged to (whoever's it was) is ended instead of being left alive and unreachable.
    /// </param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    public async Task<IssuedSession> StartAsync(User user, string? previousRefreshToken, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokenSettings = settings.Value;
        var refreshToken = RefreshToken.StartSession(
            user.Id,
            now,
            tokenSettings.RefreshTokenLifetime,
            tokenSettings.AbsoluteSessionLifetime);

        await EndPreviousSessionAsync(previousRefreshToken, now, cancellationToken);
        await PruneEndedSessionsAsync(user.Id, now, cancellationToken);
        dbContext.RefreshTokens.Add(refreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IssuedSession(user, accessTokens.Issue(user), refreshToken);
    }

    private async Task EndPreviousSessionAsync(string? previousRefreshToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (previousRefreshToken is null)
        {
            return;
        }

        if (await revoker.RevokeSessionOfAsync(previousRefreshToken, now, cancellationToken) is { } previousUserId)
        {
            LogPreviousSessionEnded(previousUserId);
        }
    }

    // Tokens from sessions past their absolute end can never be refreshed, and replaying them is already
    // just "expired", so they are no longer needed for reuse detection. Pruning on sign-in keeps the
    // table bounded per user without a background job.
    private Task<int> PruneEndedSessionsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.SessionExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

    [LoggerMessage(Level = LogLevel.Information, Message = "Earlier session of user {UserId} ended by a new sign-in in the same browser")]
    private partial void LogPreviousSessionEnded(Guid userId);
}

internal sealed record IssuedSession(User User, AccessToken AccessToken, IssuedRefreshToken RefreshToken);
