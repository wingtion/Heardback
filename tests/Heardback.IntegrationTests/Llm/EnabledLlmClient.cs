using Heardback.Infrastructure.Llm;

namespace Heardback.IntegrationTests.Llm;

/// <summary>Reports the LLM as enabled without any provider, so jobs can be queued; never called by job tests.</summary>
internal sealed class EnabledLlmClient : ILlmClient
{
    public bool IsEnabled => true;

    public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Job tests must not call the LLM.");
}
