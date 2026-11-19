using System.Diagnostics.CodeAnalysis;

namespace Heardback.Infrastructure.Llm;

/// <summary>Result of <see cref="IStructuredLlm.RunAsync{TInput, TOutput}"/>.</summary>
/// <typeparam name="T">The answer type.</typeparam>
public sealed record LlmStructuredResult<T>
    where T : class
{
    private LlmStructuredResult(LlmOutcome outcome, T? value, LlmCompletion? completion, TimeSpan? retryAfter)
    {
        Outcome = outcome;
        Value = value;
        Completion = completion;
        RetryAfter = retryAfter;
    }

    /// <summary>How the run ended.</summary>
    public LlmOutcome Outcome { get; }

    /// <summary>The validated answer, when <see cref="Outcome"/> is <see cref="LlmOutcome.Succeeded"/>.</summary>
    public T? Value { get; }

    /// <summary>Which provider and model gave <see cref="Value"/>, and its token counts.</summary>
    public LlmCompletion? Completion { get; }

    /// <summary>For <see cref="LlmOutcome.Unavailable"/>: the earliest wait a provider asked for, if any.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>True when <see cref="Value"/> holds a validated answer.</summary>
    [MemberNotNullWhen(true, nameof(Value), nameof(Completion))]
    public bool IsSuccess => Outcome == LlmOutcome.Succeeded;

    internal static LlmStructuredResult<T> Success(T value, LlmCompletion completion) =>
        new(LlmOutcome.Succeeded, value, completion, null);

    internal static LlmStructuredResult<T> Failure(LlmOutcome outcome, TimeSpan? retryAfter = null) =>
        new(outcome, null, null, retryAfter);
}
