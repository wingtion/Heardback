namespace Heardback.Infrastructure.Llm;

/// <summary>A model's answer. <paramref name="Content"/> is unvalidated JSON text and must be checked before use.</summary>
/// <param name="Provider">The provider that answered, such as <c>groq</c>.</param>
/// <param name="Model">The model that answered.</param>
/// <param name="Content">The raw answer text.</param>
/// <param name="InputTokens">Tokens in the request, when the provider reports them.</param>
/// <param name="OutputTokens">Tokens in the answer, when the provider reports them.</param>
public sealed record LlmCompletion(string Provider, string Model, string Content, int? InputTokens, int? OutputTokens);
