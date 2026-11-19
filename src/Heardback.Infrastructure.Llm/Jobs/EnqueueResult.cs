namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>Result of <see cref="IAnalysisJobs.EnqueueAsync"/>.</summary>
public enum EnqueueResult
{
    /// <summary>A new job was created.</summary>
    Queued,

    /// <summary>An unfinished job for the same kind and subject already exists; nothing was added.</summary>
    AlreadyQueued,

    /// <summary>No LLM provider is configured, so nothing is queued (ADR-0008).</summary>
    LlmDisabled,
}
