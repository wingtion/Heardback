using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Sessions;

internal sealed class RefreshTokenRevoker(IdentityDbContext dbContext)
{
    /// <summary>
    /// Ends the session a refresh token belongs to (its whole family). Returns the session's user, or
    /// null when the token is unknown.
    /// </summary>
    public async Task<Guid?> RevokeSessionOfAsync(string plainToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshToken.Hash(plainToken);
        var token = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(candidate => candidate.TokenHash == tokenHash)
            .Select(candidate => new { candidate.UserId, candidate.FamilyId })
            .SingleOrDefaultAsync(cancellationToken);
        if (token is null)
        {
            return null;
        }

        await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
        return token.UserId;
    }

    /// <summary>Ends one session: every still-active token descending from a single login.</summary>
    public Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);

    /// <summary>Ends every session the user has, on every device.</summary>
    public Task<int> RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
}
