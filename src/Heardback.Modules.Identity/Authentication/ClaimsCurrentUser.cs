using Heardback.Shared.Users;
using Microsoft.AspNetCore.Http;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>
/// Reads the current user from the validated access token's claims. A scope that set a <see cref="BackgroundUser"/>
/// (a background job, or seeding a new demo account) runs as that user instead, and never as an admin or a demo
/// visitor with its limits.
/// </summary>
internal sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor, BackgroundUser backgroundUser)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            // Set explicitly for this scope, so it wins over the request's token, which in the demo sign-up
            // request is anonymous.
            if (backgroundUser.UserId is { } scopeUser)
            {
                return scopeUser;
            }

            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirst(IdentityClaims.Subject)?.Value, out var userId) ? userId : null;
        }
    }

    public bool IsAdmin => IsTokenUser && httpContextAccessor.HttpContext!.User.IsInRole(IdentityClaims.AdminRole);

    public bool IsDemo =>
        IsTokenUser && httpContextAccessor.HttpContext!.User.HasClaim(IdentityClaims.AccountKind, IdentityClaims.DemoAccount);

    private bool IsTokenUser =>
        backgroundUser.UserId is null
        && httpContextAccessor.HttpContext is not null
        && UserId is not null;
}
