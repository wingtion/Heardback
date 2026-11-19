using System.Net;
using System.Text.Json;
using Heardback.Infrastructure.Llm.Providers;
using Heardback.Infrastructure.Llm.Providers.Gemini;

namespace Heardback.UnitTests.Llm;

public sealed class GeminiProviderTests
{
    private const string BaseUrl = "https://gemini.test/v1beta/";

    private const string Answer = """
        {
          "candidates": [{
            "content": { "role": "model", "parts": [
              { "text": "Thinking about the fields...", "thought": true },
              { "text": "{\"title\":" },
              { "text": "\"Senior .NET developer\"}" }
            ] },
            "finishReason": "STOP"
          }],
          "usageMetadata": { "promptTokenCount": 95, "candidatesTokenCount": 21, "totalTokenCount": 116 }
        }
        """;

    [Fact]
    public async Task SendAsync_ValidAnswer_JoinsAnswerPartsWithoutThoughts()
    {
        var provider = CreateProvider(StubHttpHandler.Json(Answer));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Answered, reply.Kind);
        Assert.NotNull(reply.Completion);
        Assert.Equal("""{"title":"Senior .NET developer"}""", reply.Completion.Content);
        Assert.Equal(GeminiProvider.ProviderName, reply.Completion.Provider);
        Assert.Equal(95, reply.Completion.InputTokens);
        Assert.Equal(21, reply.Completion.OutputTokens);
    }

    [Fact]
    public async Task SendAsync_Request_SendsSchemaInstructionsAndKeyHeader()
    {
        var handler = StubHttpHandler.Json(Answer);
        var provider = CreateProvider(handler);

        await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(new Uri(BaseUrl + "models/gemini-3.8-flash:generateContent"), handler.LastRequest.RequestUri);
        Assert.Equal([LlmTestData.GeminiKey], handler.LastRequest.Headers.GetValues("x-goog-api-key"));
        Assert.DoesNotContain(LlmTestData.GeminiKey, handler.LastRequest.RequestUri!.ToString(), StringComparison.Ordinal);

        using var body = JsonDocument.Parse(handler.LastBody!);
        var root = body.RootElement;
        Assert.Equal(
            LlmTestData.Instructions,
            root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        var content = root.GetProperty("contents")[0];
        Assert.Equal("user", content.GetProperty("role").GetString());
        Assert.Equal(LlmTestData.Input, content.GetProperty("parts")[0].GetProperty("text").GetString());
        var config = root.GetProperty("generationConfig");
        Assert.Equal("application/json", config.GetProperty("responseMimeType").GetString());
        Assert.Equal(512, config.GetProperty("maxOutputTokens").GetInt32());
        Assert.True(JsonElement.DeepEquals(LlmTestData.Schema, config.GetProperty("responseJsonSchema")));
    }

    [Fact]
    public async Task SendAsync_PromptBlocked_IsRejected()
    {
        var provider = CreateProvider(StubHttpHandler.Json("""{"promptFeedback": {"blockReason": "SAFETY"}}"""));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Rejected, reply.Kind);
        Assert.Equal(ProviderHttp.Blocked, reply.Reason);
    }

    [Fact]
    public async Task SendAsync_AnswerWithheldForSafety_IsRejected()
    {
        var provider = CreateProvider(StubHttpHandler.Json("""{"candidates": [{"finishReason": "SAFETY"}]}"""));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Rejected, reply.Kind);
    }

    [Theory]
    [InlineData("""{"candidates": []}""")]
    [InlineData("""{"candidates": [{"content": {"parts": [{"text": "only thinking", "thought": true}]}}]}""")]
    public async Task SendAsync_NoAnswerText_IsTransient(string body)
    {
        var provider = CreateProvider(StubHttpHandler.Json(body));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
        Assert.Equal(ProviderHttp.EmptyAnswer, reply.Reason);
    }

    [Fact]
    public async Task SendAsync_RateLimited_IsTransient()
    {
        var provider = CreateProvider(StubHttpHandler.Status(HttpStatusCode.TooManyRequests));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
    }

    private static GeminiProvider CreateProvider(StubHttpHandler handler) =>
        new(LlmTestData.Client(handler, BaseUrl), LlmTestData.Settings(), TimeProvider.System);
}
