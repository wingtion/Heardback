using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Recovery;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Members;

/// <summary>
/// Lets an admin give a member a reset code. Never for an admin, so one admin cannot take over another; admins
/// recover with the owner command (ADR-0015).
/// </summary>
internal sealed partial class IssueResetCodeHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    ResetCodeIssuer issuer,
    ILogger<IssueResetCodeHandler> logger)
{
    public async Task<Result<ResetCodeResponse>> HandleAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var adminId = currentUser.GetRequiredUserId();
        var member = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == memberId)
            .Select(user => new { user.IsAdmin })
            .FirstOrDefaultAsync(cancellationToken);
        if (member is null)
        {
            return IdentityErrors.MemberNotFound;
        }

        if (member.IsAdmin)
        {
            return IdentityErrors.AdminResetNotAllowed;
        }

        var code = await issuer.IssueAsync(memberId, adminId, cancellationToken);
        LogIssued(adminId, memberId);
        return code;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Admin {AdminId} issued a reset code for user {UserId}")]
    private partial void LogIssued(Guid adminId, Guid userId);
}
