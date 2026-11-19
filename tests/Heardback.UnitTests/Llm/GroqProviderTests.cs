using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Heardback.Infrastructure.Llm.Providers;
using Heardback.Infrastructure.Llm.Providers.Groq;
using Microsoft.Extensions.Time.Testing;

namespace Heardback.UnitTests.Llm;

public sealed class GroqProviderTests
{
    private const string BaseUrl = "https://api.groq.test/openai/v1/";

    private const string Answer = """
        {
          "choices": [{ "message": { "role": "assistant", "content": "{\"title\":\"Senior .NET developer\"}" },
                        "finish_reason": "stop" }],
          "usage": { "prompt_tokens": 120, "completion_tokens": 18, "total_tokens": 138 }
        }
        """;

    [Fact]
    public async Task SendAsync_ValidAnswer_ReturnsContentAndTokenCounts()
    {
        var provider = CreateProvider(StubHttpHandler.Json(Answer));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Answered, reply.Kind);
        Assert.NotNull(reply.Completion);
        Assert.Equal("""{"title":"Senior .NET developer"}""", reply.Completion.Content);
        Assert.Equal(GroqProvider.ProviderName, reply.Completion.Provider);
        Assert.Equal("openai/gpt-oss-120b", reply.Completion.Model);
        Assert.Equal(120, reply.Completion.InputTokens);
        Assert.Equal(18, reply.Completion.OutputTokens);
    }

    [Fact]
    public async Task SendAsync_Request_SendsStrictSchemaMessagesAndBearerKey()
    {
        var handler = StubHttpHandler.Json(Answer);
        var provider = CreateProvider(handler);

        await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal(new Uri(BaseUrl + "chat/completions"), handler.LastRequest.RequestUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", LlmTestData.GroqKey), handler.LastRequest.Headers.Authorization);

        using var body = JsonDocument.Parse(handler.LastBody!);
        var root = body.RootElement;
        Assert.Equal("openai/gpt-oss-120b", root.GetProperty("model").GetString());
        Assert.Equal(512, root.GetProperty("max_completion_tokens").GetInt32());
        Assert.Equal("low", root.GetProperty("reasoning_effort").GetString());
        var messages = root.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal(LlmTestData.Instructions, messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal(LlmTestData.Input, messages[1].GetProperty("content").GetString());
        var format = root.GetProperty("response_format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        var schema = format.GetProperty("json_schema");
        Assert.Equal("posting_analysis", schema.GetProperty("name").GetString());
        Assert.True(schema.GetProperty("strict").GetBoolean());
        Assert.True(JsonElement.DeepEquals(LlmTestData.Schema, schema.GetProperty("schema")));
    }

    [Fact]
    public async Task SendAsync_RateLimitedWithRetryAfterSeconds_IsTransientWithThatWait()
    {
        var provider = CreateProvider(StubHttpHandler.Status(
            HttpStatusCode.TooManyRequests,
            response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7))));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
        Assert.Equal(429, reply.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(7), reply.RetryAfter);
    }

    [Fact]
    public async Task SendAsync_RateLimitedWithRetryAfterDate_IsTransientWithWaitUntilThen()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero));
        var handler = StubHttpHandler.Status(
            HttpStatusCode.TooManyRequests,
            response => response.Headers.RetryAfter =
                new RetryConditionHeaderValue(time.GetUtcNow().AddSeconds(90)));
        var provider = CreateProvider(handler, time);

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromSeconds(90), reply.RetryAfter);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData((HttpStatusCode)498)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SendAsync_ServerSideFailure_IsTransient(HttpStatusCode status)
    {
        var provider = CreateProvider(StubHttpHandler.Status(status));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
        Assert.Equal((int)status, reply.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task SendAsync_ClientError_IsRejected(HttpStatusCode status)
    {
        var provider = CreateProvider(StubHttpHandler.Status(status));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Rejected, reply.Kind);
        Assert.Equal((int)status, reply.StatusCode);
    }

    [Fact]
    public async Task SendAsync_NetworkFailure_IsTransient()
    {
        var provider = CreateProvider(StubHttpHandler.Throws(new HttpRequestException("connection refused")));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
        Assert.Equal(ProviderHttp.NetworkError, reply.Reason);
    }

    [Fact]
    public async Task SendAsync_ClientTimeout_IsTransient()
    {
        var provider = CreateProvider(StubHttpHandler.Throws(new TaskCanceledException("timed out")));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
        Assert.Equal(ProviderHttp.Timeout, reply.Reason);
    }

    [Fact]
    public async Task SendAsync_CallerCancels_Throws()
    {
        var provider = CreateProvider(StubHttpHandler.Json(Answer));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.SendAsync(LlmTestData.Request(), new CancellationToken(canceled: true)));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"choices": "wrong shape"}""")]
    public async Task SendAsync_MalformedResponse_IsTransient(string body)
    {
        var provider = CreateProvider(StubHttpHandler.Json(body));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
        Assert.Equal(ProviderHttp.MalformedResponse, reply.Reason);
    }

    [Theory]
    [InlineData("""{"choices": []}""")]
    [InlineData("""{"choices": [{ "message": { "content": "" } }]}""")]
    [InlineData("""{"choices": [{ "message": { "content": null } }]}""")]
    public async Task SendAsync_NoAnswerText_IsTransient(string body)
    {
        var provider = CreateProvider(StubHttpHandler.Json(body));

        var reply = await provider.SendAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReplyKind.Transient, reply.Kind);
        Assert.Equal(ProviderHttp.EmptyAnswer, reply.Reason);
    }

    private static GroqProvider CreateProvider(StubHttpHandler handler, TimeProvider? time = null) =>
        new(LlmTestData.Client(handler, BaseUrl), LlmTestData.Settings(), time ?? TimeProvider.System);
}
