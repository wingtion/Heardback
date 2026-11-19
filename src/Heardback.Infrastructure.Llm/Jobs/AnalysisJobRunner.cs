using Heardback.Infrastructure.Llm.Persistence;
using Heardback.Infrastructure.Llm.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>
/// Claims one due job, runs its handler and records the outcome. Claims use <c>FOR UPDATE SKIP LOCKED</c> and a
/// lease token, so several workers never run the same job at once and a worker that lost its lease cannot
/// overwrite the job's newer state.
/// </summary>
internal sealed partial class AnalysisJobRunner(
    LlmDbContext dbContext,
    IServiceScopeFactory scopeFactory,
    IOptions<AnalysisJobSettings> options,
    TimeProvider timeProvider,
    ILogger<AnalysisJobRunner> logger)
{
    public const string NoHandler = "no_handler";
    public const string HandlerException = "handler_exception";
    public const string LeaseExpired = "lease_expired";

    private readonly AnalysisJobSettings _settings = options.Value;

    /// <summary>Runs the next due job, if any.</summary>
    /// <returns>True when a job was run, so the caller can look for another one straight away.</returns>
    public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        await FailExhaustedLeasesAsync(cancellationToken);

        var job = await ClaimAsync(cancellationToken);
        if (job is null)
        {
            return false;
        }

        var outcome = await RunHandlerAsync(job, cancellationToken);
        await CompleteAsync(job, outcome, cancellationToken);
        return true;
    }

    private async Task<ClaimedJob?> ClaimAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var leaseToken = Guid.NewGuid();
        var leaseUntil = now + _settings.Lease;

        // A Running job whose lease expired lost its worker (a crash or restart) and may be claimed again.
        var claimed = await dbContext.Database.SqlQuery<ClaimedJob>(
            $"""
            UPDATE llm.analysis_jobs AS job
            SET status = 'Running', attempts = job.attempts + 1, lease_token = {leaseToken},
                lease_until = {leaseUntil}, updated_at = {now}
            WHERE job.id = (
                SELECT candidate.id FROM llm.analysis_jobs AS candidate
                WHERE candidate.attempts < {_settings.MaxAttempts}
                  AND ((candidate.status = 'Pending' AND candidate.next_run_at <= {now})
                    OR (candidate.status = 'Running' AND candidate.lease_until < {now}))
                ORDER BY candidate.next_run_at, candidate.id
                LIMIT 1
                FOR UPDATE SKIP LOCKED)
            RETURNING job.id, job.kind, job.subject_key, job.requested_by_user_id, job.attempts, job.lease_token
            """)
            .ToListAsync(cancellationToken);

        return claimed.SingleOrDefault();
    }

    private async Task<AnalysisJobOutcome> RunHandlerAsync(ClaimedJob job, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IAnalysisJobHandler>()
            .Where(handler => handler.Kind == job.Kind)
            .ToList();
        if (handlers.Count > 1)
        {
            throw new InvalidOperationException($"More than one handler is registered for job kind '{job.Kind}'.");
        }

        if (handlers is not [var handler])
        {
            return AnalysisJobOutcome.Failed(NoHandler);
        }

        var context = new AnalysisJobContext(job.Id, job.Kind, job.SubjectKey, job.RequestedByUserId, job.Attempts);
        try
        {
            return await handler.RunAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down: put the job back without counting this run.
            await ReleaseAsync(job, CancellationToken.None);
            throw;
        }
#pragma warning disable CA1031 // A handler bug must not stop the worker; the job is retried and then fails.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            LogHandlerException(job.Id, job.Kind, job.Attempts, exception.GetType().Name);
            return AnalysisJobOutcome.RetryLater(HandlerException);
        }
    }

    private async Task CompleteAsync(ClaimedJob job, AnalysisJobOutcome outcome, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var reason = Truncate(outcome.Reason);
        var current = dbContext.AnalysisJobs.Where(row =>
            row.Id == job.Id && row.LeaseToken == job.LeaseToken && row.Status == AnalysisJobStatus.Running);

        var kind = outcome.Kind == AnalysisJobOutcomeKind.RetryLater && job.Attempts >= _settings.MaxAttempts
            ? AnalysisJobOutcomeKind.Failed
            : outcome.Kind;

        var updated = kind switch
        {
            AnalysisJobOutcomeKind.Succeeded => await current.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, AnalysisJobStatus.Succeeded)
                    .SetProperty(row => row.LastError, (string?)null)
                    .SetProperty(row => row.LeaseToken, (Guid?)null)
                    .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(row => row.UpdatedAt, now),
                cancellationToken),
            AnalysisJobOutcomeKind.Failed => await current.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, AnalysisJobStatus.Failed)
                    .SetProperty(row => row.LastError, reason)
                    .SetProperty(row => row.LeaseToken, (Guid?)null)
                    .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(row => row.UpdatedAt, now),
                cancellationToken),
            AnalysisJobOutcomeKind.RetryLater => await current.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, AnalysisJobStatus.Pending)
                    .SetProperty(row => row.NextRunAt, now + RetryDelay(job.Attempts, outcome.RetryAfter))
                    .SetProperty(row => row.LastError, reason)
                    .SetProperty(row => row.LeaseToken, (Guid?)null)
                    .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(row => row.UpdatedAt, now),
                cancellationToken),
            AnalysisJobOutcomeKind.Deferred => await current.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, AnalysisJobStatus.Pending)
                    .SetProperty(row => row.NextRunAt, outcome.Until!.Value)
                    .SetProperty(row => row.Attempts, row => row.Attempts - 1)
                    .SetProperty(row => row.LastError, reason)
                    .SetProperty(row => row.LeaseToken, (Guid?)null)
                    .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(row => row.UpdatedAt, now),
                cancellationToken),
            _ => throw new InvalidOperationException($"Unknown job outcome {outcome.Kind}."),
        };

        if (updated == 0)
        {
            LogLeaseLost(job.Id, job.Kind);
            return;
        }

        LogFinished(job.Id, job.Kind, job.Attempts, kind, reason);
    }

    private Task<int> ReleaseAsync(ClaimedJob job, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return dbContext.AnalysisJobs
            .Where(row => row.Id == job.Id && row.LeaseToken == job.LeaseToken && row.Status == AnalysisJobStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, AnalysisJobStatus.Pending)
                    .SetProperty(row => row.Attempts, row => row.Attempts - 1)
                    .SetProperty(row => row.NextRunAt, now)
                    .SetProperty(row => row.LeaseToken, (Guid?)null)
                    .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(row => row.UpdatedAt, now),
                cancellationToken);
    }

    /// <summary>A job that kept losing its worker would otherwise be claimed forever.</summary>
    private Task<int> FailExhaustedLeasesAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return dbContext.AnalysisJobs
            .Where(row => row.Status == AnalysisJobStatus.Running
                && row.LeaseUntil < now
                && row.Attempts >= _settings.MaxAttempts)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Status, AnalysisJobStatus.Failed)
                    .SetProperty(row => row.LastError, LeaseExpired)
                    .SetProperty(row => row.LeaseToken, (Guid?)null)
                    .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(row => row.UpdatedAt, now),
                cancellationToken);
    }

    private TimeSpan RetryDelay(int attempt, TimeSpan? retryAfter) =>
        JobBackoff.Delay(attempt, _settings.FirstRetryDelay, _settings.MaxRetryDelay, Random.Shared.NextDouble(), retryAfter);

    private static string? Truncate(string? reason) =>
        reason is { Length: > AnalysisJob.ErrorMaxLength } ? reason[..AnalysisJob.ErrorMaxLength] : reason;

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobId} ({Kind}) attempt {Attempt}: {Outcome} {Reason}")]
    private partial void LogFinished(Guid jobId, string kind, int attempt, AnalysisJobOutcomeKind outcome, string? reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} ({Kind}) attempt {Attempt} threw {ExceptionType}")]
    private partial void LogHandlerException(Guid jobId, string kind, int attempt, string exceptionType);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Job {JobId} ({Kind}) finished after its lease was taken over; the result was not recorded")]
    private partial void LogLeaseLost(Guid jobId, string kind);
}
