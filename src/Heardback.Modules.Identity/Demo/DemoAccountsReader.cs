using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.Demo;

/// <summary>Serves <see cref="IDemoAccounts"/>. A few dozen rows at most: the visitor cap plus the neighbors.</summary>
internal sealed class DemoAccountsReader(IdentityDbContext dbContext) : IDemoAccounts
{
    public async Task<DemoAccountIds> ListAsync(CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Kind != UserKind.Member)
            .Select(user => new { user.Id, user.Kind })
            .ToListAsync(cancellationToken);

        return new DemoAccountIds(
            [.. accounts.Where(account => account.Kind == UserKind.DemoVisitor).Select(account => account.Id)],
            [.. accounts.Where(account => account.Kind == UserKind.DemoNeighbor).Select(account => account.Id)]);
    }
}
