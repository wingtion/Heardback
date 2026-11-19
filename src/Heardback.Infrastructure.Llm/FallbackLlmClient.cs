using Heardback.Infrastructure.Llm.Providers;
using Microsoft.Extensions.Logging;

namespace Heardback.Infrastructure.Llm;

/// <summary>
/// Tries the configured providers in order and moves on only for transient failures. Requests with CV text
/// skip providers that may not receive personal data (ADR-0008).
/// </summary>
internal sealed partial class FallbackLlmClient(
    IEnumerable<ILlmProvider> providers,
    TimeProvider timeProvider,
    ILogger<FallbackLlmClient> logger) : ILlmClient
{
    private readonly ILlmProvider[] _configured = [.. providers.Where(provider => provider.IsConfigured)];

    public bool IsEnabled => _configured.Length > 0;

    public async Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
        {
            return LlmResult.Failure(LlmOutcome.Disabled);
        }

        var eligible = _configured
            .Where(provider => request.DataClass == LlmDataClass.Public || provider.AllowsPersonalData)
            .ToList();
        if (eligible.Count == 0)
        {
            LogNoEligibleProvider(request.PromptId, request.PromptVersion, request.DataClass);
            return LlmResult.Failure(LlmOutcome.NoEligibleProvider);
        }

        TimeSpan? retryAfter = null;
        foreach (var provider in eligible)
        {
            var reply = await SendAsync(provider, request, cancellationToken);
            switch (reply.Kind)
            {
                case ProviderReplyKind.Answered:
                    return LlmResult.Success(reply.Completion!);
                case ProviderReplyKind.Rejected:
                    return LlmResult.Failure(LlmOutcome.Rejected);
                case ProviderReplyKind.Transient:
                    retryAfter = Earliest(retryAfter, reply.RetryAfter);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown provider reply kind {reply.Kind}.");
            }
        }

        return LlmResult.Unavailable(retryAfter);
    }

    private async Task<ProviderReply> SendAsync(
        ILlmProvider provider,
        LlmRequest request,
        CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();
        var reply = await provider.SendAsync(request, cancellationToken);
        var elapsedMs = (long)timeProvider.GetElapsedTime(started).TotalMilliseconds;

        if (reply.Completion is { } completion)
        {
            LogAnswered(
                provider.Name, provider.Model, request.PromptId, request.PromptVersion, elapsedMs,
                completion.InputTokens, completion.OutputTokens);
        }
        else
        {
            LogNotAnswered(
                provider.Name, provider.Model, request.PromptId, request.PromptVersion, reply.Kind, elapsedMs,
                reply.StatusCode, reply.Reason);
        }

        return reply;
    }

    private static TimeSpan? Earliest(TimeSpan? current, TimeSpan? next) =>
        (current, next) switch
        {
            (null, _) => next,
            (_, null) => current,
            _ => current < next ? current : next,
        };

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "LLM call to {Provider} ({Model}) for {PromptId} v{PromptVersion} answered in {ElapsedMs} ms, "
            + "tokens in {InputTokens} out {OutputTokens}")]
    private partial void LogAnswered(
        string provider, string model, string promptId, int promptVersion, long elapsedMs,
        int? inputTokens, int? outputTokens);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "LLM call to {Provider} ({Model}) for {PromptId} v{PromptVersion} failed ({Outcome}) in "
            + "{ElapsedMs} ms, status {StatusCode}, reason {Reason}")]
    private partial void LogNotAnswered(
        string provider, string model, string promptId, int promptVersion, ProviderReplyKind outcome,
        long elapsedMs, int? statusCode, string? reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No configured LLM provider may receive {DataClass} data for {PromptId} v{PromptVersion}")]
    private partial void LogNoEligibleProvider(string promptId, int promptVersion, LlmDataClass dataClass);
}
