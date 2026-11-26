using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Paging;
using Heardback.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Members;

internal sealed class ListMembersHandler(IdentityDbContext dbContext, TimeProvider timeProvider)
{
    public Task<PagedResponse<MemberResponse>> HandleAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return dbContext.Users
            .AsNoTracking()
            .Where(user => user.Kind == UserKind.Member)
            .OrderBy(user => user.CreatedAt)
            .ThenBy(user => user.NormalizedEmail)
            .Select(user => new MemberResponse(
                user.Id,
                user.DisplayName,
                user.Email,
                user.IsAdmin,
                user.CreatedAt,
                dbContext.PasswordResetCodes
                    .Where(code => code.UserId == user.Id && code.UsedAt == null && code.ExpiresAt > now)
                    .Select(code => (DateTimeOffset?)code.ExpiresAt)
                    .FirstOrDefault()))
            .ToPagedResponseAsync(page, cancellationToken);
    }
}
