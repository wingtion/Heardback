using System.ComponentModel.DataAnnotations;

namespace Heardback.Infrastructure.Llm.Settings;

/// <summary>Background job worker settings (ADR-0008).</summary>
internal sealed class AnalysisJobSettings
{
    public const string SectionName = "Llm:Jobs";

    /// <summary>Off in tests that drive the runner themselves. The worker also stays idle while the LLM is disabled.</summary>
    public bool RunWorker { get; set; } = true;

    /// <summary>How often an idle worker looks for due jobs.</summary>
    [Range(typeof(TimeSpan), "00:00:00.050", "00:10:00")]
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long a claim lasts before another worker may take the job over. Longer than one run can take: two
    /// providers, two answers each, at their timeout.
    /// </summary>
    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Runs before a job is marked failed.</summary>
    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 6;

    /// <summary>Wait after the first transient failure; doubles with each attempt.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan FirstRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Longest wait between attempts.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromHours(1);
}
