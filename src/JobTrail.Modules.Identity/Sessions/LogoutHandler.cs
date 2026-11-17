using JobTrail.Modules.Identity.Domain;
using JobTrail.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobTrail.Modules.Identity.Sessions;

/// <summary>
/// Ends the session the refresh cookie belongs to. Other devices keep their own sessions. The current
/// access token stays valid until it expires (at most the access-token lifetime).
/// </summary>
internal sealed partial class LogoutHandler(
    IdentityDbContext dbContext,
    RefreshTokenRevoker revoker,
    TimeProvider timeProvider,
    ILogger<LogoutHandler> logger)
{
    public async Task HandleAsync(string? plainToken, CancellationToken cancellationToken)
    {
        if (plainToken is null)
        {
            return;
        }

        var tokenHash = RefreshToken.Hash(plainToken);
        var token = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(candidate => candidate.TokenHash == tokenHash)
            .Select(candidate => new { candidate.UserId, candidate.FamilyId })
            .SingleOrDefaultAsync(cancellationToken);
        if (token is null)
        {
            return;
        }

        await revoker.RevokeFamilyAsync(token.FamilyId, timeProvider.GetUtcNow(), cancellationToken);
        LogSignedOut(token.UserId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} signed out")]
    private partial void LogSignedOut(Guid userId);
}
