using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Demo;

/// <summary>
/// Creates the permanent demo neighbors once, with their sample data. Neighbors that already exist are left alone,
/// so it runs on every startup.
/// </summary>
internal sealed partial class DemoNeighborProvisioner(
    IdentityDbContext dbContext,
    DemoAccountScope demoScope,
    TimeProvider timeProvider,
    ILogger<DemoNeighborProvisioner> logger)
{
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        for (var number = 1; number <= DemoNeighbors.Count; number++)
        {
            var normalizedEmail = User.NormalizeEmail(User.DemoNeighborEmail(number));
            if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            {
                continue;
            }

            var neighbor = User.CreateDemoNeighbor(number, timeProvider.GetUtcNow());
            dbContext.Users.Add(neighbor);
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await demoScope.SeedNeighborAsync(neighbor.Id, number, cancellationToken);
            }
            catch
            {
                // Without its data the neighbor is useless; remove it so the next startup tries again.
                await demoScope.RemoveAsync(neighbor.Id, CancellationToken.None);
                throw;
            }

            LogCreated(number);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo neighbor {Number} created")]
    private partial void LogCreated(int number);
}
