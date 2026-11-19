namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>
/// The Postgres-backed queue for LLM work (ADR-0008). A job holds only a kind and a subject key (an id or a
/// hash), never user text: its handler loads the current data when it runs.
/// </summary>
public interface IAnalysisJobs
{
    /// <summary>
    /// Queues a job unless an unfinished one for the same <paramref name="kind"/> and
    /// <paramref name="subjectKey"/> exists. Safe under concurrent calls: at most one is created.
    /// </summary>
    /// <param name="kind">The handler's <see cref="IAnalysisJobHandler.Kind"/>.</param>
    /// <param name="subjectKey">What the job is about, such as a content hash or an application id.</param>
    /// <param name="requestedByUserId">The user whose action queued it, for quota refunds; null for shared work.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Whether a job was created.</returns>
    Task<EnqueueResult> EnqueueAsync(
        string kind,
        string subjectKey,
        Guid? requestedByUserId,
        CancellationToken cancellationToken);

    /// <summary>The most recent job for <paramref name="kind"/> and <paramref name="subjectKey"/>, or null.</summary>
    /// <param name="kind">The handler's kind.</param>
    /// <param name="subjectKey">What the job is about.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The latest job's state, or null when none was ever queued.</returns>
    Task<AnalysisJobState?> GetLatestAsync(string kind, string subjectKey, CancellationToken cancellationToken);
}
