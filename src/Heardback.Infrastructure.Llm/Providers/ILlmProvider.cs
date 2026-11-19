namespace Heardback.Infrastructure.Llm.Providers;

/// <summary>One provider's HTTP API. Registered in fallback order.</summary>
internal interface ILlmProvider
{
    string Name { get; }

    string Model { get; }

    bool IsConfigured { get; }

    bool AllowsPersonalData { get; }

    /// <summary>Sends one request. Transport failures and timeouts are returned, caller cancellation is thrown.</summary>
    Task<ProviderReply> SendAsync(LlmRequest request, CancellationToken cancellationToken);
}
