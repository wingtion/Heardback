using System.Security.Cryptography;
using System.Text;
using JobTrail.Modules.Identity.Domain;
using JobTrail.Modules.Identity.Persistence;
using JobTrail.Modules.Identity.Sessions;
using JobTrail.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace JobTrail.Modules.Identity.Registration;

internal sealed partial class RegisterUserHandler(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    SessionIssuer sessions,
    IOptions<InviteSettings> inviteSettings,
    TimeProvider timeProvider,
    ILogger<RegisterUserHandler> logger)
{
    // Arbitrary application-wide key for pg_advisory_xact_lock; only bootstrap registration uses it.
    private const long BootstrapAdminLockKey = 0x4A6F_6254_7261_696C;

    public async Task<Result<IssuedSession>> HandleAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var code = InviteCode.Normalize(request.InviteCode);

        // Check the invite before the email, so the endpoint cannot be used to probe for accounts without one.
        // These checks are a fast path only; the authoritative ones run inside the transaction below.
        var registersAdmin = IsBootstrapCode(code);
        var inviteLooksUsable = registersAdmin
            ? !await AdminExistsAsync(cancellationToken)
            : await IsActiveInviteAsync(code, now, cancellationToken);
        if (!inviteLooksUsable)
        {
            return IdentityErrors.InvalidInviteCode;
        }

        var normalizedEmail = User.NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return IdentityErrors.EmailTaken;
        }

        var user = User.Create(request.Email, request.DisplayName, registersAdmin, now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (registersAdmin && !await TryReserveBootstrapAdminAsync(cancellationToken))
        {
            // Another registration claimed the bootstrap code a moment ago.
            return IdentityErrors.InvalidInviteCode;
        }

        if (!await TryAddUserAsync(user, cancellationToken))
        {
            return IdentityErrors.EmailTaken;
        }

        if (!registersAdmin && !await TryClaimInviteAsync(code, user.Id, now, cancellationToken))
        {
            // Someone else used the code between the check and now.
            return IdentityErrors.InvalidInviteCode;
        }

        var session = await sessions.StartAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogRegistered(user.Id, registersAdmin);
        return session;
    }

    private bool IsBootstrapCode(string code)
    {
        var bootstrapCode = inviteSettings.Value.NormalizedBootstrapAdminCode;
        return bootstrapCode is not null
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(code), Encoding.UTF8.GetBytes(bootstrapCode));
    }

    private Task<bool> AdminExistsAsync(CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.IsAdmin, cancellationToken);

    // The advisory lock is held until the transaction ends, so concurrent bootstrap registrations queue up
    // here and each one re-checks for an admin only after the previous one has committed.
    private async Task<bool> TryReserveBootstrapAdminAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock({BootstrapAdminLockKey})",
            cancellationToken);
        return !await AdminExistsAsync(cancellationToken);
    }

    private Task<bool> IsActiveInviteAsync(string code, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.InviteCodes.AnyAsync(
            invite => invite.Code == code && invite.UsedByUserId == null && invite.ExpiresAt > now,
            cancellationToken);

    private async Task<bool> TryAddUserAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    // A single conditional UPDATE, so two registrations can never both claim the same code.
    private async Task<bool> TryClaimInviteAsync(
        string code,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var claimed = await dbContext.InviteCodes
            .Where(invite => invite.Code == code && invite.UsedByUserId == null && invite.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(invite => invite.UsedByUserId, userId)
                    .SetProperty(invite => invite.UsedAt, now),
                cancellationToken);

        return claimed == 1;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} registered (admin: {IsAdmin})")]
    private partial void LogRegistered(Guid userId, bool isAdmin);
}
