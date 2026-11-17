using Heardback.Modules.Identity.CurrentUser;
using Heardback.Modules.Identity.Sessions;

namespace Heardback.Modules.Identity.Endpoints;

/// <summary>Returned on register, login and refresh. The refresh token travels only in its httpOnly cookie.</summary>
internal sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, CurrentUserResponse User)
{
    public static AuthResponse From(IssuedSession session) =>
        new(session.AccessToken.Value, session.AccessToken.ExpiresAt, CurrentUserResponse.From(session.User));
}
