using System.Text.Json;

namespace Heardback.Infrastructure.Llm.Providers.Gemini;

// Gemini's generateContent format; serialized in camelCase (see GeminiProvider).

internal sealed record GeminiRequest(
    GeminiContent SystemInstruction,
    IReadOnlyList<GeminiContent> Contents,
    GeminiGenerationConfig GenerationConfig);

internal sealed record GeminiContent(string? Role, IReadOnlyList<GeminiPart>? Parts);

/// <param name="Text">The part's text.</param>
/// <param name="Thought">True for a thinking model's reasoning summary, which is not part of the answer.</param>
internal sealed record GeminiPart(string? Text, bool? Thought = null);

internal sealed record GeminiGenerationConfig(string ResponseMimeType, JsonElement ResponseJsonSchema, int MaxOutputTokens);

internal sealed record GeminiResponse(
    IReadOnlyList<GeminiCandidate>? Candidates,
    GeminiPromptFeedback? PromptFeedback,
    GeminiUsage? UsageMetadata);

internal sealed record GeminiCandidate(GeminiContent? Content, string? FinishReason);

internal sealed record GeminiPromptFeedback(string? BlockReason);

internal sealed record GeminiUsage(int? PromptTokenCount, int? CandidatesTokenCount);
