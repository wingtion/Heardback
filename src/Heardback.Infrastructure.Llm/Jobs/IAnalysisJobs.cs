using Heardback.Infrastructure.Llm.Quotas;

namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>
/// The Postgres-backed queue for LLM work (ADR-0008). A job holds only a kind and a subject key (an id or a
/// hash), never user text: its handler loads the current data when it runs.
/// </summary>
public interface IAnalysisJobs
{
    /// <summary>
    /// Queues a job unless an unfinished one for the same <paramref name="kind"/> and
    /// <paramref name="subjectKey"/> exists. Safe under concurrent calls: at most one is created. Counts against no
    /// quota; for work a user asks for, use <see cref="EnqueueForUserAsync"/>.
    /// </summary>
    /// <param name="kind">The handler's <see cref="IAnalysisJobHandler.Kind"/>.</param>
    /// <param name="subjectKey">What the job is about, such as a content hash or an application id.</param>
    /// <param name="requestedByUserId">The user the job runs for, if any.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Whether a job was created.</returns>
    Task<EnqueueResult> EnqueueAsync(
        string kind,
        string subjectKey,
        Guid? requestedByUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Counts one use of <paramref name="kind"/> against the user's daily limit and queues the job, in one
    /// transaction: when the limit is used up nothing is queued, and when an unfinished job already exists nothing
    /// is counted. The use is given back if the job finally fails.
    /// </summary>
    /// <param name="kind">The handler's kind; also the quota's name.</param>
    /// <param name="subjectKey">What the job is about.</param>
    /// <param name="userId">The signed-in user (from the token); the job runs for them.</param>
    /// <param name="dailyLimit">Uses allowed per UTC day.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Whether a job was created.</returns>
    Task<EnqueueResult> EnqueueForUserAsync(
        string kind,
        string subjectKey,
        Guid userId,
        int dailyLimit,
        CancellationToken cancellationToken);

    /// <summary>The user's use of <paramref name="kind"/> today.</summary>
    /// <param name="kind">The quota's name.</param>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="dailyLimit">Uses allowed per UTC day.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Uses counted, the limit, and when it resets.</returns>
    Task<LlmUsage> GetUsageAsync(string kind, Guid userId, int dailyLimit, CancellationToken cancellationToken);

    /// <summary>The most recent job for <paramref name="kind"/> and <paramref name="subjectKey"/>, or null.</summary>
    /// <param name="kind">The handler's kind.</param>
    /// <param name="subjectKey">What the job is about.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The latest job's state, or null when none was ever queued.</returns>
    Task<AnalysisJobState?> GetLatestAsync(string kind, string subjectKey, CancellationToken cancellationToken);
}
