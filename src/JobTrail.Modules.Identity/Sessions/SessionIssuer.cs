using JobTrail.Modules.Identity.Authentication;
using JobTrail.Modules.Identity.Domain;
using JobTrail.Modules.Identity.Persistence;
using Microsoft.Extensions.Options;

namespace JobTrail.Modules.Identity.Sessions;

/// <summary>Starts a signed-in session: a new refresh-token family plus a fresh access token.</summary>
internal sealed class SessionIssuer(
    IdentityDbContext dbContext,
    AccessTokenIssuer accessTokens,
    IOptions<TokenSettings> settings,
    TimeProvider timeProvider)
{
    public async Task<IssuedSession> StartAsync(User user, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var refreshToken = RefreshToken.Issue(user.Id, familyId: Guid.CreateVersion7(now), now, settings.Value.RefreshTokenLifetime);

        dbContext.RefreshTokens.Add(refreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IssuedSession(user, accessTokens.Issue(user), refreshToken);
    }
}

internal sealed record IssuedSession(User User, AccessToken AccessToken, IssuedRefreshToken RefreshToken);
