using Heardback.Infrastructure.Llm;

namespace Heardback.UnitTests.Llm;

/// <summary>An <see cref="ILlmClient"/> that returns prepared results in order and records the requests.</summary>
internal sealed class ScriptedLlmClient(params LlmResult[] results) : ILlmClient
{
    private readonly Queue<LlmResult> _results = new(results);

    public List<LlmRequest> Requests { get; } = [];

    public bool IsEnabled => true;

    public static LlmResult Answer(string content) =>
        LlmResult.Success(new LlmCompletion("groq", "groq-model", content, 10, 5));

    public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_results.Dequeue());
    }
}
