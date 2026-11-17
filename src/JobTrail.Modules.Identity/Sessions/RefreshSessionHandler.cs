using JobTrail.Modules.Identity.Authentication;
using JobTrail.Modules.Identity.Domain;
using JobTrail.Modules.Identity.Persistence;
using JobTrail.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobTrail.Modules.Identity.Sessions;

/// <summary>
/// Exchanges a refresh token for a new access token and a new refresh token (rotation). Presenting a token
/// that was already rotated, outside the short grace period, revokes every token from that login.
/// </summary>
internal sealed partial class RefreshSessionHandler(
    IdentityDbContext dbContext,
    AccessTokenIssuer accessTokens,
    RefreshTokenRevoker revoker,
    IOptions<TokenSettings> settings,
    TimeProvider timeProvider,
    ILogger<RefreshSessionHandler> logger)
{
    public async Task<Result<IssuedSession>> HandleAsync(string? plainToken, CancellationToken cancellationToken)
    {
        if (plainToken is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var tokenHash = RefreshToken.Hash(plainToken);
        var current = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (current is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var now = timeProvider.GetUtcNow();
        var state = current.GetState(now, settings.Value.RefreshReuseGracePeriod);
        if (state == RefreshTokenState.Reused)
        {
            await revoker.RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            LogReuseDetected(current.UserId, current.FamilyId);
            return IdentityErrors.InvalidRefreshToken;
        }

        if (state != RefreshTokenState.Active)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == current.UserId, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var next = await TryRotateAsync(current, now, cancellationToken);
        return next is null
            ? IdentityErrors.InvalidRefreshToken
            : new IssuedSession(user, accessTokens.Issue(user), next);
    }

    // Insert the successor, then retire the current token with a conditional UPDATE: if a concurrent
    // refresh got there first, nothing is updated and this attempt is rolled back.
    private async Task<IssuedRefreshToken?> TryRotateAsync(
        RefreshToken current,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var next = RefreshToken.Issue(current.UserId, current.FamilyId, now, settings.Value.RefreshTokenLifetime);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.RefreshTokens.Add(next.Token);
        await dbContext.SaveChangesAsync(cancellationToken);

        var retired = await dbContext.RefreshTokens
            .Where(token => token.Id == current.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.ReplacedByTokenId, next.Token.Id),
                cancellationToken);
        if (retired != 1)
        {
            return null;
        }

        await transaction.CommitAsync(cancellationToken);
        return next;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; revoked token family {FamilyId}")]
    private partial void LogReuseDetected(Guid userId, Guid familyId);
}
