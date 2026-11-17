using JobTrail.Modules.Identity.Authentication;
using JobTrail.Modules.Identity.Domain;
using JobTrail.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobTrail.Modules.Identity.Sessions;

/// <summary>
/// Starts a signed-in session: a new refresh-token family (with its absolute expiry fixed now) plus a fresh
/// access token.
/// </summary>
internal sealed class SessionIssuer(
    IdentityDbContext dbContext,
    AccessTokenIssuer accessTokens,
    IOptions<TokenSettings> settings,
    TimeProvider timeProvider)
{
    public async Task<IssuedSession> StartAsync(User user, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokenSettings = settings.Value;
        var refreshToken = RefreshToken.StartSession(
            user.Id,
            now,
            tokenSettings.RefreshTokenLifetime,
            tokenSettings.AbsoluteSessionLifetime);

        await PruneEndedSessionsAsync(user.Id, now, cancellationToken);
        dbContext.RefreshTokens.Add(refreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IssuedSession(user, accessTokens.Issue(user), refreshToken);
    }

    // Tokens from sessions past their absolute end can never be refreshed, and replaying them is already
    // just "expired", so they are no longer needed for reuse detection. Pruning on sign-in keeps the
    // table bounded per user without a background job.
    private Task<int> PruneEndedSessionsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.SessionExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
}

internal sealed record IssuedSession(User User, AccessToken AccessToken, IssuedRefreshToken RefreshToken);
