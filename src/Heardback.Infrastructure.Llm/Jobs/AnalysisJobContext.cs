namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>The job a handler is asked to run.</summary>
/// <param name="JobId">The job's id; safe to log.</param>
/// <param name="Kind">The job kind.</param>
/// <param name="SubjectKey">What the job is about.</param>
/// <param name="RequestedByUserId">The user whose action queued it, if any.</param>
/// <param name="Attempt">1 for the first run.</param>
public sealed record AnalysisJobContext(
    Guid JobId,
    string Kind,
    string SubjectKey,
    Guid? RequestedByUserId,
    int Attempt);
