using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Recovery;

/// <summary>
/// Sets a new password and ends everything the old one opened: every refresh token, every access token issued before
/// now (through <see cref="User.PasswordChangedAt"/>) and any reset code still open (ADR-0015).
/// </summary>
internal sealed class PasswordChanger(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    RefreshTokenRevoker revoker,
    TimeProvider timeProvider)
{
    /// <summary>Call inside the caller's transaction, with <paramref name="user"/> tracked.</summary>
    public async Task ChangeAsync(User user, string newPassword, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        user.ChangePassword(passwordHasher.HashPassword(user, newPassword), now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await revoker.RevokeAllForUserAsync(user.Id, now, cancellationToken);
        await dbContext.PasswordResetCodes
            .Where(code => code.UserId == user.Id && code.UsedAt == null)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
