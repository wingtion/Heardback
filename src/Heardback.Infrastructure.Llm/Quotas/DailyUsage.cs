using Heardback.Shared.Persistence;

namespace Heardback.Infrastructure.Llm.Quotas;

/// <summary>How many uses of one LLM feature a user has had on one UTC day. Written with SQL (see AnalysisJobs).</summary>
[NotUserOwned("Counted inside the enqueue transaction and by the background worker; never listed by user.")]
internal sealed class DailyUsage
{
    private DailyUsage()
    {
    }

    public Guid UserId { get; private set; }

    public DateOnly Day { get; private set; }

    /// <summary>The job kind the uses are for, such as <c>cv-tailoring</c>.</summary>
    public string Kind { get; private set; } = string.Empty;

    public int Count { get; private set; }
}
