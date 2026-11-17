using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Paging;
using Heardback.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Invites;

internal sealed class ListInviteCodesHandler(IdentityDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<PagedResponse<InviteCodeResponse>> HandleAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var rows = await dbContext.InviteCodes
            .AsNoTracking()
            .OrderByDescending(invite => invite.CreatedAt)
            .ThenByDescending(invite => invite.Id)
            .Select(invite => new InviteCodeRow(
                invite.Id,
                invite.Code,
                invite.CreatedAt,
                invite.ExpiresAt,
                invite.UsedAt))
            .ToPagedResponseAsync(page, cancellationToken);

        // Status depends on "now", so it is derived after loading rather than stored.
        var now = timeProvider.GetUtcNow();
        var items = rows.Items
            .Select(row => new InviteCodeResponse(
                row.Id,
                InviteCode.Format(row.Code),
                row.CreatedAt,
                row.ExpiresAt,
                InviteCode.StatusAt(row.UsedAt, row.ExpiresAt, now),
                row.UsedAt))
            .ToList();

        return new PagedResponse<InviteCodeResponse>(items, rows.Page, rows.PageSize, rows.TotalCount);
    }

    private sealed record InviteCodeRow(
        Guid Id,
        string Code,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? UsedAt);
}
