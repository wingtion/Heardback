using Heardback.Infrastructure.Llm.Persistence;
using Heardback.Infrastructure.Llm.Quotas;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Heardback.Infrastructure.Llm.Jobs;

internal sealed class AnalysisJobs(LlmDbContext dbContext, ILlmClient llmClient, TimeProvider timeProvider)
    : IAnalysisJobs
{
    public async Task<EnqueueResult> EnqueueAsync(
        string kind,
        string subjectKey,
        Guid? requestedByUserId,
        CancellationToken cancellationToken)
    {
        CheckKeys(kind, subjectKey);
        if (!llmClient.IsEnabled)
        {
            return EnqueueResult.LlmDisabled;
        }

        var inserted = await InsertJobAsync(kind, subjectKey, requestedByUserId, quotaDay: null, cancellationToken);
        return inserted ? EnqueueResult.Queued : EnqueueResult.AlreadyQueued;
    }

    public async Task<EnqueueResult> EnqueueForUserAsync(
        string kind,
        string subjectKey,
        Guid userId,
        int dailyLimit,
        CancellationToken cancellationToken)
    {
        CheckKeys(kind, subjectKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dailyLimit);
        if (!llmClient.IsEnabled)
        {
            return EnqueueResult.LlmDisabled;
        }

        var day = LlmDay.Of(timeProvider.GetUtcNow());
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Increments only below the limit. A concurrent request waits for the row lock and then sees the new
        // count, so two requests can never both take the last use.
        var counted = await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO llm.daily_usage AS usage (user_id, day, kind, count) VALUES ({userId}, {day}, {kind}, 1)
            ON CONFLICT (user_id, day, kind) DO UPDATE SET count = usage.count + 1 WHERE usage.count < {dailyLimit}
            """,
            cancellationToken);
        if (counted == 0)
        {
            return EnqueueResult.QuotaExceeded;
        }

        if (!await InsertJobAsync(kind, subjectKey, userId, day, cancellationToken))
        {
            // Rolling back un-counts the use: the job the user is waiting for is already queued.
            return EnqueueResult.AlreadyQueued;
        }

        await transaction.CommitAsync(cancellationToken);
        return EnqueueResult.Queued;
    }

    public async Task<LlmUsage> GetUsageAsync(
        string kind,
        Guid userId,
        int dailyLimit,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var day = LlmDay.Of(now);
        var used = await dbContext.DailyUsage
            .AsNoTracking()
            .Where(usage => usage.UserId == userId && usage.Day == day && usage.Kind == kind)
            .Select(usage => usage.Count)
            .FirstOrDefaultAsync(cancellationToken);

        return new LlmUsage(used, dailyLimit, LlmDay.NextReset(now));
    }

    public Task<AnalysisJobState?> GetLatestAsync(
        string kind,
        string subjectKey,
        CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs
            .AsNoTracking()
            .Where(job => job.Kind == kind && job.SubjectKey == subjectKey)
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .Select(job => new AnalysisJobState(job.Status, job.Attempts, job.LastError, job.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> InsertJobAsync(
        string kind,
        string subjectKey,
        Guid? requestedByUserId,
        DateOnly? quotaDay,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var requestedBy = new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Uuid,
            Value = (object?)requestedByUserId ?? DBNull.Value,
        };
        var day = new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Date, Value = (object?)quotaDay ?? DBNull.Value };

        // ON CONFLICT names the partial unique index's predicate, so concurrent calls create at most one job.
        var inserted = await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO llm.analysis_jobs
                (id, kind, subject_key, status, attempts, next_run_at, requested_by_user_id, quota_day,
                 created_at, updated_at)
            VALUES ({Guid.CreateVersion7()}, {kind}, {subjectKey}, 'Pending', 0, {now}, {requestedBy}, {day},
                    {now}, {now})
            ON CONFLICT (kind, subject_key) WHERE status IN ('Pending', 'Running') DO NOTHING
            """,
            cancellationToken);
        return inserted == 1;
    }

    private static void CheckKeys(string kind, string subjectKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectKey);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(kind.Length, AnalysisJob.KindMaxLength, nameof(kind));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            subjectKey.Length, AnalysisJob.SubjectKeyMaxLength, nameof(subjectKey));
    }
}
