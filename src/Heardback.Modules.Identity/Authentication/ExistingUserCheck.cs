using Heardback.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>
/// Rejects an access token whose user no longer exists. Without it, a deleted account's last access token would keep
/// working, and could write new data, until it expires (ADR-0015). One primary-key lookup per request.
/// </summary>
internal static class ExistingUserCheck
{
    private const string DeletedUserFailure = "The user of this token no longer exists.";

    public static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirst(IdentityClaims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            context.Fail(DeletedUserFailure);
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IdentityDbContext>();
        var exists = await dbContext.Users.AnyAsync(user => user.Id == userId, context.HttpContext.RequestAborted);
        if (!exists)
        {
            context.Fail(DeletedUserFailure);
        }
    }
}
