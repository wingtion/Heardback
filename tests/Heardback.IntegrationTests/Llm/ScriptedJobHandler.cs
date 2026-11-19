using System.Collections.Concurrent;
using Heardback.Infrastructure.Llm.Jobs;

namespace Heardback.IntegrationTests.Llm;

/// <summary>A job handler whose behaviour each test sets, recording every run.</summary>
internal sealed class ScriptedJobHandler : IAnalysisJobHandler
{
    public const string TestKind = "test-kind";

    public Func<AnalysisJobContext, CancellationToken, Task<AnalysisJobOutcome>> Behaviour { get; set; } =
        (_, _) => Task.FromResult(AnalysisJobOutcome.Succeeded());

    public ConcurrentQueue<AnalysisJobContext> Runs { get; } = new();

    /// <summary>The job scope's services, when registered per scope (see AnalysisJobsTests).</summary>
    public IServiceProvider? Services { get; set; }

    public string Kind => TestKind;

    public Task<AnalysisJobOutcome> RunAsync(AnalysisJobContext job, CancellationToken cancellationToken)
    {
        Runs.Enqueue(job);
        return Behaviour(job, cancellationToken);
    }
}
