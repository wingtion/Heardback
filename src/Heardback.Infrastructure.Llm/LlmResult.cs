using System.Diagnostics.CodeAnalysis;

namespace Heardback.Infrastructure.Llm;

/// <summary>Result of an <see cref="ILlmClient"/> call.</summary>
public sealed record LlmResult
{
    private LlmResult(LlmOutcome outcome, LlmCompletion? completion, TimeSpan? retryAfter)
    {
        Outcome = outcome;
        Completion = completion;
        RetryAfter = retryAfter;
    }

    /// <summary>How the call ended.</summary>
    public LlmOutcome Outcome { get; }

    /// <summary>The answer, when <see cref="Outcome"/> is <see cref="LlmOutcome.Succeeded"/>.</summary>
    public LlmCompletion? Completion { get; }

    /// <summary>For <see cref="LlmOutcome.Unavailable"/>: the earliest wait a provider asked for, if any.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>True when a provider answered.</summary>
    [MemberNotNullWhen(true, nameof(Completion))]
    public bool IsSuccess => Outcome == LlmOutcome.Succeeded;

    internal static LlmResult Success(LlmCompletion completion) => new(LlmOutcome.Succeeded, completion, null);

    internal static LlmResult Unavailable(TimeSpan? retryAfter) => new(LlmOutcome.Unavailable, null, retryAfter);

    internal static LlmResult Failure(LlmOutcome outcome) => new(outcome, null, null);
}
