using System.Text.Json;

namespace Heardback.Infrastructure.Llm;

/// <summary>One call to a language model that must answer with JSON matching <paramref name="ResponseSchema"/>.</summary>
/// <param name="PromptId">Stable prompt name, such as <c>posting-analysis</c>; logged, never the prompt text.</param>
/// <param name="PromptVersion">Prompt version; logged and stored with results.</param>
/// <param name="DataClass">What the request carries; decides which providers may receive it.</param>
/// <param name="Instructions">The system prompt.</param>
/// <param name="Input">The user content the instructions apply to.</param>
/// <param name="SchemaName">Name of the response schema (letters, digits, <c>_</c> and <c>-</c>).</param>
/// <param name="ResponseSchema">
/// JSON schema of the answer. Every property is listed in <c>required</c> and every object sets
/// <c>additionalProperties: false</c>, which Groq's strict mode requires.
/// </param>
/// <param name="MaxOutputTokens">Upper bound on the answer's length, in tokens.</param>
/// <param name="Reasoning">Reasoning effort for this request; <see cref="LlmReasoning.Default"/> uses the setting.</param>
public sealed record LlmRequest(
    string PromptId,
    int PromptVersion,
    LlmDataClass DataClass,
    string Instructions,
    string Input,
    string SchemaName,
    JsonElement ResponseSchema,
    int MaxOutputTokens,
    LlmReasoning Reasoning = LlmReasoning.Default);
