using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Demo;

/// <summary>Removes demo visitors whose time is up, with all their data, the same way an account deletion does.</summary>
internal sealed partial class DemoAccountCleaner(
    IdentityDbContext dbContext,
    DemoAccountScope demoScope,
    TimeProvider timeProvider,
    ILogger<DemoAccountCleaner> logger)
{
    public async Task<int> RemoveExpiredAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expired = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Kind == UserKind.DemoVisitor && user.ExpiresAt <= now)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in expired)
        {
            await demoScope.RemoveAsync(userId, cancellationToken);
        }

        if (expired.Count > 0)
        {
            LogRemoved(expired.Count);
        }

        return expired.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed {Count} expired demo visitors")]
    private partial void LogRemoved(int count);
}
