namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>Where a background analysis job stands. Stored by name.</summary>
public enum AnalysisJobStatus
{
    /// <summary>Waiting for its next run time.</summary>
    Pending,

    /// <summary>Claimed by a worker under a lease.</summary>
    Running,

    /// <summary>Finished; the handler stored its result.</summary>
    Succeeded,

    /// <summary>Gave up: a permanent failure, or every attempt used.</summary>
    Failed,
}
