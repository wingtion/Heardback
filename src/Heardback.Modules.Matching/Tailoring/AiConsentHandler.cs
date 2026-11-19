using Heardback.Modules.Matching.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// Reads and changes the user's permission to send their CV text to the AI provider. Withdrawing it also deletes
/// every tailoring suggestion made from the CV; queued jobs then end without an LLM call.
/// </summary>
internal sealed partial class AiConsentHandler(
    MatchingDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<AiConsentHandler> logger)
{
    public async Task<AiConsentResponse> GetAsync(CancellationToken cancellationToken)
    {
        var grantedAt = await dbContext.AiConsents
            .AsNoTracking()
            .Select(consent => (DateTimeOffset?)consent.GrantedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return new AiConsentResponse(grantedAt is not null, grantedAt);
    }

    public async Task<AiConsentResponse> SetAsync(bool allowed, CancellationToken cancellationToken)
    {
        if (allowed)
        {
            if (!await dbContext.AiConsents.AnyAsync(cancellationToken))
            {
                dbContext.AiConsents.Add(AiConsent.Grant(timeProvider.GetUtcNow()));
                await SaveIgnoringDuplicateAsync(cancellationToken);
                LogGranted();
            }

            return await GetAsync(cancellationToken);
        }

        // The ownership filter limits both statements to the user's own rows.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var withdrawn = await dbContext.AiConsents.ExecuteDeleteAsync(cancellationToken);
        await dbContext.TailoringResults.ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (withdrawn > 0)
        {
            LogWithdrawn();
        }

        return new AiConsentResponse(false, null);
    }

    // Two tabs allowing at once: the second insert hits the primary key, and the user has consented either way.
    private async Task SaveIgnoringDuplicateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "AI consent for CV text granted")]
    private partial void LogGranted();

    [LoggerMessage(Level = LogLevel.Information, Message = "AI consent for CV text withdrawn; tailoring results deleted")]
    private partial void LogWithdrawn();
}
