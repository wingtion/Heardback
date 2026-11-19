namespace Heardback.Infrastructure.Llm;

/// <summary>
/// How much a reasoning model should think before answering. More effort follows instructions more closely but
/// costs more output tokens and time. Only providers that support it use it (Groq); others ignore it.
/// </summary>
public enum LlmReasoning
{
    /// <summary>The provider setting (<c>Llm:Groq:ReasoningEffort</c>).</summary>
    Default,

    Low,

    Medium,

    High,
}
