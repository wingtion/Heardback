using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Privacy;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Account;

/// <summary>Collects everything the signed-in user can see about themselves into one export (ADR-0015).</summary>
internal sealed partial class ExportAccountHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IEnumerable<IUserDataExporter> exporters,
    TimeProvider timeProvider,
    ILogger<ExportAccountHandler> logger)
{
    public const int FormatVersion = 1;

    public async Task<Result<AccountExport>> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var profile = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AccountProfileExport(user.Email, user.DisplayName, user.IsAdmin, user.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var data = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var exporter in exporters)
        {
            data.Add(exporter.Section, await exporter.ExportAsync(userId, cancellationToken));
        }

        LogExported(userId);
        return new AccountExport(FormatVersion, timeProvider.GetUtcNow(), profile, data);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} exported their data")]
    private partial void LogExported(Guid userId);
}
