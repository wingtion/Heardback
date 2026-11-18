using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Sessions;

/// <summary>
/// Ends the session the refresh cookie belongs to. Other devices keep their own sessions. The current
/// access token stays valid until it expires (at most the access-token lifetime).
/// </summary>
internal sealed partial class LogoutHandler(
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

        var userId = await revoker.RevokeSessionOfAsync(plainToken, timeProvider.GetUtcNow(), cancellationToken);
        if (userId is { } signedOut)
        {
            LogSignedOut(signedOut);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} signed out")]
    private partial void LogSignedOut(Guid userId);
}
