using Heardback.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>
/// Rejects an access token whose user no longer exists or changed their password after it was issued. Without it, the
/// last access token of a deleted account or of a reset password would keep working, and could write data, until it
/// expires (ADR-0015). One primary-key lookup per request.
/// </summary>
internal static class ExistingUserCheck
{
    private const string StaleTokenFailure = "The user of this token no longer exists or changed their password.";

    public static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirst(IdentityClaims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            context.Fail(StaleTokenFailure);
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IdentityDbContext>();
        var user = await dbContext.Users.AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new { candidate.PasswordChangedAt })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (user is null || IssuedBefore(context.SecurityToken.ValidFrom, user.PasswordChangedAt))
        {
            context.Fail(StaleTokenFailure);
        }
    }

    // Tokens carry whole seconds, so the change time is cut to its second: the new session issued in the same second
    // as the change keeps working. A token issued earlier in that same second does too; that is under a second.
    private static bool IssuedBefore(DateTime issuedAtUtc, DateTimeOffset? passwordChangedAt) =>
        passwordChangedAt is { } changedAt
        && new DateTimeOffset(issuedAtUtc, TimeSpan.Zero).ToUnixTimeSeconds() < changedAt.ToUnixTimeSeconds();
}
