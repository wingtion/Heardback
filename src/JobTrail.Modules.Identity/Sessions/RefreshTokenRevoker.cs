using JobTrail.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobTrail.Modules.Identity.Sessions;

internal sealed class RefreshTokenRevoker(IdentityDbContext dbContext)
{
    /// <summary>Revokes every still-active token from one login, ending that session everywhere it was refreshed.</summary>
    public Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
}
