using System.Collections.Concurrent;
using Heardback.Infrastructure.Llm;

namespace Heardback.IntegrationTests.Llm;

/// <summary>An enabled LLM client that answers from <see cref="Respond"/> and records every request.</summary>
internal sealed class CannedLlmClient : ILlmClient
{
    public Func<LlmRequest, LlmResult> Respond { get; set; } = _ => throw new InvalidOperationException("No answer set.");

    public ConcurrentQueue<LlmRequest> Requests { get; } = new();

    public bool IsEnabled => true;

    public static LlmResult Answer(string json) =>
        LlmResult.Success(new LlmCompletion("groq", "openai/gpt-oss-120b", json, 300, 80));

    public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        return Task.FromResult(Respond(request));
    }
}
