namespace Heardback.Infrastructure.Llm.Quotas;

/// <summary>The app-wide daily cap on LLM calls, which protects the shared free tier from one user or a bug.</summary>
internal interface ILlmCallBudget
{
    /// <summary>Counts one call if today's cap allows it.</summary>
    /// <returns>Null when the call may go ahead; otherwise when the cap resets.</returns>
    Task<DateTimeOffset?> TryUseAsync(CancellationToken cancellationToken);
}
