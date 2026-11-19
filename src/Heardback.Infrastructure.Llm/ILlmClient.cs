namespace Heardback.Infrastructure.Llm;

/// <summary>
/// Sends a request to the first available provider that may receive its data class, falling back to the next
/// one on rate limits, server errors and timeouts (ADR-0008).
/// </summary>
public interface ILlmClient
{
    /// <summary>False when no provider is configured; callers then skip LLM work entirely.</summary>
    bool IsEnabled { get; }

    /// <summary>Sends <paramref name="request"/>. Expected failures are returned, not thrown.</summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The answer, or why there is none.</returns>
    Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken cancellationToken);
}
