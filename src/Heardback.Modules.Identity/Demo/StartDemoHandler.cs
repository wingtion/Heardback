using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Sessions;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Identity.Demo;

/// <summary>
/// "Try the demo" (ADR-0018): creates a demo visitor with sample data and signs the browser in, without a password
/// or an invite. At most <see cref="DemoSettings.MaxActiveVisitors"/> exist at once.
/// </summary>
internal sealed partial class StartDemoHandler(
    IdentityDbContext dbContext,
    DemoAccountScope demoScope,
    SessionIssuer sessionIssuer,
    IOptions<DemoSettings> settings,
    TimeProvider timeProvider,
    ILogger<StartDemoHandler> logger)
{
    private const long DemoPlacesLockKey = 0x4862_4465_6D6F_5374;

    public async Task<Result<IssuedSession>> HandleAsync(string? previousRefreshToken, CancellationToken cancellationToken)
    {
        if (!settings.Value.Enabled)
        {
            return IdentityErrors.DemoDisabled;
        }

        var reserved = await ReservePlaceAsync(cancellationToken);
        if (!reserved.IsSuccess)
        {
            return reserved.Error;
        }

        var visitor = reserved.Value;
        try
        {
            await demoScope.SeedVisitorAsync(visitor.Id, cancellationToken);
        }
        catch
        {
            // A half-seeded demo would look broken; remove it now rather than leave it until it expires.
            await demoScope.RemoveAsync(visitor.Id, CancellationToken.None);
            throw;
        }

        var session = await sessionIssuer.StartAsync(visitor, previousRefreshToken, cancellationToken);
        LogStarted(visitor.Id);
        return session;
    }

    // Serialized with an advisory lock, so two visitors arriving together cannot both take the last place.
    private async Task<Result<User>> ReservePlaceAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({DemoPlacesLockKey})", cancellationToken);

        var activeEndings = await dbContext.Users
            .Where(user => user.Kind == UserKind.DemoVisitor && user.ExpiresAt > now)
            .Select(user => user.ExpiresAt!.Value)
            .ToListAsync(cancellationToken);
        if (activeEndings.Count >= settings.Value.MaxActiveVisitors)
        {
            LogFull(activeEndings.Count);
            return IdentityErrors.DemoFull(activeEndings.Min());
        }

        var visitor = User.CreateDemoVisitor(now, settings.Value.Lifetime);
        dbContext.Users.Add(visitor);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return visitor;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo visitor {UserId} started")]
    private partial void LogStarted(Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Demo refused: all {ActiveVisitors} places are in use")]
    private partial void LogFull(int activeVisitors);
}
