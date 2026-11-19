namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>A handler's verdict on its job.</summary>
public sealed record AnalysisJobOutcome
{
    private AnalysisJobOutcome(AnalysisJobOutcomeKind kind, string? reason, TimeSpan? retryAfter, DateTimeOffset? until)
    {
        Kind = kind;
        Reason = reason;
        RetryAfter = retryAfter;
        Until = until;
    }

    /// <summary>What to do with the job.</summary>
    public AnalysisJobOutcomeKind Kind { get; }

    /// <summary>A short machine-readable reason, such as <c>llm_unavailable</c>. Stored and logged; never user text.</summary>
    public string? Reason { get; }

    /// <summary>For <see cref="AnalysisJobOutcomeKind.RetryLater"/>: the shortest wait before the next run.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>For <see cref="AnalysisJobOutcomeKind.Deferred"/>: when to run again.</summary>
    public DateTimeOffset? Until { get; }

    /// <summary>The job is done.</summary>
    public static AnalysisJobOutcome Succeeded() => new(AnalysisJobOutcomeKind.Succeeded, null, null, null);

    /// <summary>Run again after a backoff, but not sooner than <paramref name="retryAfter"/>.</summary>
    /// <param name="reason">Why it failed.</param>
    /// <param name="retryAfter">The provider's requested wait, if any.</param>
    /// <returns>The outcome.</returns>
    public static AnalysisJobOutcome RetryLater(string reason, TimeSpan? retryAfter = null) =>
        new(AnalysisJobOutcomeKind.RetryLater, reason, retryAfter, null);

    /// <summary>Run again at <paramref name="until"/>, without counting this run as an attempt.</summary>
    /// <param name="reason">Why it waits.</param>
    /// <param name="until">When to run again.</param>
    /// <returns>The outcome.</returns>
    public static AnalysisJobOutcome Deferred(string reason, DateTimeOffset until) =>
        new(AnalysisJobOutcomeKind.Deferred, reason, null, until);

    /// <summary>Give up now.</summary>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The outcome.</returns>
    public static AnalysisJobOutcome Failed(string reason) => new(AnalysisJobOutcomeKind.Failed, reason, null, null);
}
