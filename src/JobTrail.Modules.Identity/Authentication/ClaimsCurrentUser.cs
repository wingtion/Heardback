using JobTrail.Shared.Users;
using Microsoft.AspNetCore.Http;

namespace JobTrail.Modules.Identity.Authentication;

/// <summary>Reads the current user from the validated access token's claims.</summary>
internal sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirst(IdentityClaims.Subject)?.Value, out var userId) ? userId : null;
        }
    }

    public bool IsAdmin => UserId is not null && httpContextAccessor.HttpContext!.User.IsInRole(IdentityClaims.AdminRole);
}
