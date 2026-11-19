namespace Heardback.Infrastructure.Llm;

/// <summary>
/// Runs an <see cref="ILlmPrompt{TInput, TOutput}"/> and returns a parsed, validated answer. An answer that
/// fails parsing or validation is retried once; a second failure is <see cref="LlmOutcome.InvalidOutput"/>
/// (PLAN.md section 5).
/// </summary>
public interface IStructuredLlm
{
    /// <summary>False when no provider is configured; callers then skip LLM work entirely.</summary>
    bool IsEnabled { get; }

    /// <summary>Runs <paramref name="prompt"/> on <paramref name="input"/>.</summary>
    /// <typeparam name="TInput">The prompt's input type.</typeparam>
    /// <typeparam name="TOutput">The prompt's answer type.</typeparam>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="input">The feature's input.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The validated answer, or why there is none.</returns>
    Task<LlmStructuredResult<TOutput>> RunAsync<TInput, TOutput>(
        ILlmPrompt<TInput, TOutput> prompt,
        TInput input,
        CancellationToken cancellationToken)
        where TOutput : class;
}
