using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Heardback.Infrastructure.Llm.Settings;
using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Providers.Groq;

/// <summary>Groq's chat completions API with strict JSON schema output.</summary>
internal sealed class GroqProvider(HttpClient httpClient, IOptions<LlmSettings> options, TimeProvider timeProvider)
    : ILlmProvider
{
    public const string ProviderName = "groq";

    private const string CompletionsPath = "chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly LlmProviderSettings _settings = options.Value.Groq;

    public string Name => ProviderName;

    public string Model => _settings.Model;

    public bool IsConfigured => _settings.IsConfigured;

    public bool AllowsPersonalData => _settings.AllowsPersonalData;

    public Task<ProviderReply> SendAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = new GroqChatRequest(
            _settings.Model,
            [new GroqMessage("system", request.Instructions), new GroqMessage("user", request.Input)],
            new GroqResponseFormat("json_schema", new GroqJsonSchema(request.SchemaName, true, request.ResponseSchema)),
            request.MaxOutputTokens,
            Temperature: 0,
            _settings.ReasoningEffort);

        var message = new HttpRequestMessage(HttpMethod.Post, CompletionsPath)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        return ProviderHttp.SendAsync<GroqChatResponse>(
            httpClient, message, JsonOptions, ReadAnswer, timeProvider, cancellationToken);
    }

    private ProviderReply ReadAnswer(GroqChatResponse response)
    {
        var content = response.Choices is [{ Message.Content: { } text }, ..] ? text : null;
        if (string.IsNullOrWhiteSpace(content))
        {
            return ProviderReply.Transient(ProviderHttp.EmptyAnswer);
        }

        return ProviderReply.Answered(new LlmCompletion(
            ProviderName,
            _settings.Model,
            content,
            response.Usage?.PromptTokens,
            response.Usage?.CompletionTokens));
    }
}
