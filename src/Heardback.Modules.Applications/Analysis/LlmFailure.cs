using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>Turns a failed LLM run into what the job queue should do next.</summary>
internal static class LlmFailure
{
    public const string Unavailable = "llm_unavailable";
    public const string Disabled = "llm_disabled";
    public const string NoEligibleProvider = "no_eligible_provider";
    public const string Rejected = "llm_rejected";
    public const string InvalidOutput = "invalid_output";

    public static AnalysisJobOutcome ToJobOutcome(LlmOutcome outcome, TimeSpan? retryAfter) =>
        outcome switch
        {
            LlmOutcome.Unavailable => AnalysisJobOutcome.RetryLater(Unavailable, retryAfter),
            LlmOutcome.Disabled => AnalysisJobOutcome.Failed(Disabled),
            LlmOutcome.NoEligibleProvider => AnalysisJobOutcome.Failed(NoEligibleProvider),
            LlmOutcome.Rejected => AnalysisJobOutcome.Failed(Rejected),

            // The answer was already retried once inside IStructuredLlm (PLAN.md section 5).
            LlmOutcome.InvalidOutput => AnalysisJobOutcome.Failed(InvalidOutput),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Not a failure."),
        };
}
