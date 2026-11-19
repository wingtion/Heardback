namespace Heardback.Infrastructure.Llm;

/// <summary>How an <see cref="ILlmClient"/> call ended.</summary>
public enum LlmOutcome
{
    /// <summary>A provider answered; the answer still needs validating.</summary>
    Succeeded,

    /// <summary>No provider is configured: LLM features are off.</summary>
    Disabled,

    /// <summary>Providers are configured, but none may receive this data class.</summary>
    NoEligibleProvider,

    /// <summary>Every eligible provider was rate-limited, down or timed out. Worth retrying later.</summary>
    Unavailable,

    /// <summary>A provider rejected the request itself (bad key, bad request, blocked content). Retrying will not help.</summary>
    Rejected,
}
