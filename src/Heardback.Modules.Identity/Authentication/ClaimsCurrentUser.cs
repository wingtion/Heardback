using Heardback.Shared.Users;
using Microsoft.AspNetCore.Http;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>
/// Reads the current user from the validated access token's claims. Outside a request (a background job's scope)
/// it is the <see cref="BackgroundUser"/> the job queue set from the job row, never an admin.
/// </summary>
internal sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor, BackgroundUser backgroundUser)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return backgroundUser.UserId;
            }

            var principal = httpContext.User;
            if (principal.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirst(IdentityClaims.Subject)?.Value, out var userId) ? userId : null;
        }
    }

    public bool IsAdmin =>
        httpContextAccessor.HttpContext is { } httpContext
        && UserId is not null
        && httpContext.User.IsInRole(IdentityClaims.AdminRole);
}
