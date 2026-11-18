using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Paging;
using Heardback.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Invites;

internal sealed class ListInviteCodesHandler(IdentityDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<InviteCodeListResponse> HandleAsync(PageRequest page, CancellationToken cancellationToken)
    {
        // Status depends on "now", so it is derived rather than stored. One instant is used for the
        // statuses and the active count, so the two always agree.
        var now = timeProvider.GetUtcNow();

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

        var activeCount = await dbContext.InviteCodes
            .AsNoTracking()
            .CountAsync(InviteCode.IsActiveAt(now), cancellationToken);

        var items = rows.Items
            .Select(row => new InviteCodeResponse(
                row.Id,
                InviteCode.Format(row.Code),
                row.CreatedAt,
                row.ExpiresAt,
                InviteCode.StatusAt(row.UsedAt, row.ExpiresAt, now),
                row.UsedAt))
            .ToList();

        return new InviteCodeListResponse(
            items,
            rows.Page,
            rows.PageSize,
            rows.TotalCount,
            rows.TotalPages,
            activeCount);
    }

    private sealed record InviteCodeRow(
        Guid Id,
        string Code,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? UsedAt);
}
