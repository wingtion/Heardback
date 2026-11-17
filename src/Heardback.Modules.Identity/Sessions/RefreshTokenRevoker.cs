using Heardback.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Sessions;

internal sealed class RefreshTokenRevoker(IdentityDbContext dbContext)
{
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
