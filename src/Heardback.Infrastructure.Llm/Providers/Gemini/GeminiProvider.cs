using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Heardback.Infrastructure.Llm.Settings;
using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Providers.Gemini;

/// <summary>Gemini's generateContent API with JSON schema output.</summary>
internal sealed class GeminiProvider(HttpClient httpClient, IOptions<LlmSettings> options, TimeProvider timeProvider)
    : ILlmProvider
{
    public const string ProviderName = "gemini";

    private const string ApiKeyHeader = "x-goog-api-key";
    private const string JsonMimeType = "application/json";

    /// <summary>Finish reasons that mean the answer was withheld; asking again gives the same result.</summary>
    private static readonly HashSet<string> BlockedFinishReasons =
        ["SAFETY", "RECITATION", "BLOCKLIST", "PROHIBITED_CONTENT", "SPII"];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly LlmProviderSettings _settings = options.Value.Gemini;

    public string Name => ProviderName;

    public string Model => _settings.Model;

    public bool IsConfigured => _settings.IsConfigured;

    public bool AllowsPersonalData => _settings.AllowsPersonalData;

    public Task<ProviderReply> SendAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = new GeminiRequest(
            new GeminiContent(null, [new GeminiPart(request.Instructions)]),
            [new GeminiContent("user", [new GeminiPart(request.Input)])],
            new GeminiGenerationConfig(JsonMimeType, request.ResponseSchema, request.MaxOutputTokens));

        var path = $"models/{Uri.EscapeDataString(_settings.Model)}:generateContent";
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        message.Headers.Add(ApiKeyHeader, _settings.ApiKey);

        return ProviderHttp.SendAsync<GeminiResponse>(
            httpClient, message, JsonOptions, ReadAnswer, timeProvider, cancellationToken);
    }

    private ProviderReply ReadAnswer(GeminiResponse response)
    {
        if (response.PromptFeedback?.BlockReason is not null)
        {
            return ProviderReply.Rejected(ProviderHttp.Blocked);
        }

        if (response.Candidates is not [var candidate, ..])
        {
            return ProviderReply.Transient(ProviderHttp.EmptyAnswer);
        }

        if (candidate.FinishReason is { } finishReason && BlockedFinishReasons.Contains(finishReason))
        {
            return ProviderReply.Rejected(ProviderHttp.Blocked);
        }

        var answerParts = (candidate.Content?.Parts ?? [])
            .Where(part => part.Thought != true)
            .Select(part => part.Text);
        var content = string.Concat(answerParts);
        if (string.IsNullOrWhiteSpace(content))
        {
            return ProviderReply.Transient(ProviderHttp.EmptyAnswer);
        }

        return ProviderReply.Answered(new LlmCompletion(
            ProviderName,
            _settings.Model,
            content,
            response.UsageMetadata?.PromptTokenCount,
            response.UsageMetadata?.CandidatesTokenCount));
    }
}
