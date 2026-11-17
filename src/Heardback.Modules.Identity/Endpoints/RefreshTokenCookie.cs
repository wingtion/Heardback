using Heardback.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;

namespace Heardback.Modules.Identity.Endpoints;

/// <summary>
/// The refresh token lives only in this cookie: httpOnly (scripts cannot read it), Secure, SameSite=Strict,
/// and scoped to the auth endpoints so it is not sent with every API call.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "heardback_refresh";
    public const string Path = "/api/auth";

    public static void Append(HttpResponse response, IssuedRefreshToken refreshToken) =>
        response.Cookies.Append(Name, refreshToken.PlainToken, CreateOptions(refreshToken.Token.ExpiresAt));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, CreateOptions(expires: null));

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static CookieOptions CreateOptions(DateTimeOffset? expires) =>
        new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = expires,
            IsEssential = true,
        };
}
