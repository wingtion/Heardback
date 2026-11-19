namespace Heardback.Infrastructure.Llm.Quotas;

/// <summary>A user's use of one LLM feature today, for "3 of 10 left today" in the UI.</summary>
/// <param name="Used">Uses counted today (given back when a job finally fails).</param>
/// <param name="Limit">The daily limit.</param>
/// <param name="ResetsAt">Midnight UTC; the UI shows it in the user's zone.</param>
public sealed record LlmUsage(int Used, int Limit, DateTimeOffset ResetsAt)
{
    /// <summary>Uses left today.</summary>
    public int Remaining => Math.Max(0, Limit - Used);
}
