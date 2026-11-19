using Heardback.Shared.Persistence;

namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>
/// One unit of LLM work. Rows are written with SQL (see <see cref="AnalysisJobs"/> and
/// <see cref="AnalysisJobRunner"/>) so claims and inserts stay safe under concurrency; this class maps them.
/// </summary>
[NotUserOwned("Processed by the background worker, which has no signed-in user; never exposed by id.")]
internal sealed class AnalysisJob
{
    public const int KindMaxLength = 64;
    public const int SubjectKeyMaxLength = 200;
    public const int ErrorMaxLength = 64;

    private AnalysisJob()
    {
    }

    public Guid Id { get; private set; }

    public string Kind { get; private set; } = string.Empty;

    public string SubjectKey { get; private set; } = string.Empty;

    public AnalysisJobStatus Status { get; private set; }

    /// <summary>Runs started so far; a run cut short by a crash counts too.</summary>
    public int Attempts { get; private set; }

    public DateTimeOffset NextRunAt { get; private set; }

    /// <summary>Identifies the current claim; only its holder may finish the job.</summary>
    public Guid? LeaseToken { get; private set; }

    /// <summary>After this, the claim is considered lost (the worker died) and the job may be claimed again.</summary>
    public DateTimeOffset? LeaseUntil { get; private set; }

    public string? LastError { get; private set; }

    public Guid? RequestedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
