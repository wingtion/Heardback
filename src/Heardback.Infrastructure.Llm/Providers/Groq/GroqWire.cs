using System.Text.Json;

namespace Heardback.Infrastructure.Llm.Providers.Groq;

// Groq's OpenAI-compatible chat completions format; serialized in snake_case (see GroqProvider).

internal sealed record GroqChatRequest(
    string Model,
    IReadOnlyList<GroqMessage> Messages,
    GroqResponseFormat ResponseFormat,
    int MaxCompletionTokens,
    double Temperature,
    string? ReasoningEffort);

internal sealed record GroqMessage(string Role, string Content);

internal sealed record GroqResponseFormat(string Type, GroqJsonSchema JsonSchema);

internal sealed record GroqJsonSchema(string Name, bool Strict, JsonElement Schema);

internal sealed record GroqChatResponse(IReadOnlyList<GroqChoice>? Choices, GroqUsage? Usage);

internal sealed record GroqChoice(GroqAnswerMessage? Message, string? FinishReason);

internal sealed record GroqAnswerMessage(string? Content);

internal sealed record GroqUsage(int? PromptTokens, int? CompletionTokens);
