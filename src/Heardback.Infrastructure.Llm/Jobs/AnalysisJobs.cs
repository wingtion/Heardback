using Heardback.Infrastructure.Llm.Persistence;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectKey);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(kind.Length, AnalysisJob.KindMaxLength, nameof(kind));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            subjectKey.Length, AnalysisJob.SubjectKeyMaxLength, nameof(subjectKey));

        if (!llmClient.IsEnabled)
        {
            return EnqueueResult.LlmDisabled;
        }

        var now = timeProvider.GetUtcNow();
        var requestedBy = new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid, Value = (object?)requestedByUserId ?? DBNull.Value };

        // ON CONFLICT names the partial unique index's predicate, so concurrent calls create at most one job.
        var inserted = await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO llm.analysis_jobs
                (id, kind, subject_key, status, attempts, next_run_at, requested_by_user_id, created_at, updated_at)
            VALUES ({Guid.CreateVersion7()}, {kind}, {subjectKey}, 'Pending', 0, {now}, {requestedBy}, {now}, {now})
            ON CONFLICT (kind, subject_key) WHERE status IN ('Pending', 'Running') DO NOTHING
            """,
            cancellationToken);

        return inserted == 1 ? EnqueueResult.Queued : EnqueueResult.AlreadyQueued;
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
}
