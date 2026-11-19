namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>What a handler asks the queue to do with its job.</summary>
public enum AnalysisJobOutcomeKind
{
    /// <summary>Done.</summary>
    Succeeded,

    /// <summary>A transient failure: run again after a backoff, until the attempts run out.</summary>
    RetryLater,

    /// <summary>Waiting for something known, such as a quota reset; not counted as an attempt.</summary>
    Deferred,

    /// <summary>A permanent failure: give up now.</summary>
    Failed,
}
