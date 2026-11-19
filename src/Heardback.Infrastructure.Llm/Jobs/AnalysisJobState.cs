namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>The latest job for one kind and subject.</summary>
/// <param name="Status">Where the job stands.</param>
/// <param name="Attempts">Runs so far, including a current one.</param>
/// <param name="LastError">Short reason code of the last failure, if any; never user text.</param>
/// <param name="UpdatedAt">When the job last changed.</param>
public sealed record AnalysisJobState(AnalysisJobStatus Status, int Attempts, string? LastError, DateTimeOffset UpdatedAt);
