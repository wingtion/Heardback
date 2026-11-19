namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>
/// Turns a failed <see cref="IStructuredLlm"/> run into what the job queue should do next. The reason codes are
/// stored on the job and logged; they never contain user text.
/// </summary>
public static class LlmJobOutcome
{
    /// <summary>Every provider was rate-limited, down or timed out.</summary>
    public const string Unavailable = "llm_unavailable";

    /// <summary>No provider is configured.</summary>
    public const string Disabled = "llm_disabled";

    /// <summary>No configured provider may receive the request's data class.</summary>
    public const string NoEligibleProvider = "no_eligible_provider";

    /// <summary>A provider refused the request.</summary>
    public const string Rejected = "llm_rejected";

    /// <summary>The answer failed validation, also on its one retry.</summary>
    public const string InvalidOutput = "invalid_output";

    /// <summary>The app-wide daily cap is used up; the job waits until it resets.</summary>
    public const string DailyCap = "daily_cap";

    /// <summary>The queue's next step for a failed run.</summary>
    /// <typeparam name="T">The prompt's answer type.</typeparam>
    /// <param name="result">A result that is not a success.</param>
    /// <returns>Retry, defer or fail.</returns>
    public static AnalysisJobOutcome FromFailure<T>(LlmStructuredResult<T> result)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome switch
        {
            LlmOutcome.Unavailable => AnalysisJobOutcome.RetryLater(Unavailable, result.RetryAfter),
            LlmOutcome.DailyCapReached => AnalysisJobOutcome.Deferred(DailyCap, result.ResumeAt!.Value),
            LlmOutcome.Disabled => AnalysisJobOutcome.Failed(Disabled),
            LlmOutcome.NoEligibleProvider => AnalysisJobOutcome.Failed(NoEligibleProvider),
            LlmOutcome.Rejected => AnalysisJobOutcome.Failed(Rejected),

            // The answer was already retried once inside IStructuredLlm (PLAN.md section 5).
            LlmOutcome.InvalidOutput => AnalysisJobOutcome.Failed(InvalidOutput),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, "Not a failure."),
        };
    }
}
