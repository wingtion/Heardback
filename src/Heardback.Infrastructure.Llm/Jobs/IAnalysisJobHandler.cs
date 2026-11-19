namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>
/// Does the work for one job kind. Registered by the module that owns the data; resolved in a fresh scope
/// per job. Expected failures are returned as an <see cref="AnalysisJobOutcome"/>; an exception counts as a
/// transient failure.
/// </summary>
public interface IAnalysisJobHandler
{
    /// <summary>Unique job kind, such as <c>posting-analysis</c>.</summary>
    string Kind { get; }

    /// <summary>Runs one job.</summary>
    /// <param name="job">The job being run.</param>
    /// <param name="cancellationToken">Cancelled when the app shuts down; the job is then put back.</param>
    /// <returns>What to do with the job next.</returns>
    Task<AnalysisJobOutcome> RunAsync(AnalysisJobContext job, CancellationToken cancellationToken);
}
