namespace Heardback.Infrastructure.Llm;

/// <summary>
/// One versioned prompt with a typed answer. Each feature keeps its prompts in one place; changing the
/// instructions or the answer type means a new <see cref="Version"/>, which is stored with every result.
/// </summary>
/// <typeparam name="TInput">What the feature passes in.</typeparam>
/// <typeparam name="TOutput">
/// The answer record. Its JSON schema is generated from the type: every constructor parameter is required and
/// unknown properties are refused.
/// </typeparam>
public interface ILlmPrompt<in TInput, TOutput>
    where TOutput : class
{
    /// <summary>Stable name, such as <c>posting-analysis</c>. Logged and stored.</summary>
    string Id { get; }

    /// <summary>Version of the instructions and answer type together.</summary>
    int Version { get; }

    /// <summary>What the input contains; decides which providers may receive it.</summary>
    LlmDataClass DataClass { get; }

    /// <summary>Upper bound on the answer's length, in tokens.</summary>
    int MaxOutputTokens { get; }

    /// <summary>Reasoning effort; raise it for prompts whose rules a quick answer tends to break.</summary>
    LlmReasoning Reasoning { get; }

    /// <summary>The system prompt.</summary>
    string Instructions { get; }

    /// <summary>Builds the user message. Wrap user-supplied text with <see cref="LlmInput.Untrusted"/>.</summary>
    /// <param name="input">The feature's input.</param>
    /// <returns>The user message.</returns>
    string BuildInput(TInput input);

    /// <summary>
    /// Checks rules the schema cannot express (lengths, list sizes, blank strings). Returns short fixed codes
    /// such as <c>skills.too_many</c>, never text from the answer, because they are logged.
    /// </summary>
    /// <param name="output">The parsed answer.</param>
    /// <returns>The problems found; empty when the answer is usable.</returns>
    IReadOnlyList<string> FindProblems(TOutput output);
}
