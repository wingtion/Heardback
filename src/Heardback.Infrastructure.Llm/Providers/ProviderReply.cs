namespace Heardback.Infrastructure.Llm.Providers;

/// <param name="Kind">How the call ended.</param>
/// <param name="Completion">The answer, when <paramref name="Kind"/> is <see cref="ProviderReplyKind.Answered"/>.</param>
/// <param name="StatusCode">The HTTP status, when there was a response; logged.</param>
/// <param name="Reason">A short machine-readable reason (never response text); logged.</param>
/// <param name="RetryAfter">The wait the provider asked for, if any.</param>
internal sealed record ProviderReply(
    ProviderReplyKind Kind,
    LlmCompletion? Completion,
    int? StatusCode,
    string? Reason,
    TimeSpan? RetryAfter)
{
    public static ProviderReply Answered(LlmCompletion completion) =>
        new(ProviderReplyKind.Answered, completion, null, null, null);

    public static ProviderReply Transient(string reason, int? statusCode = null, TimeSpan? retryAfter = null) =>
        new(ProviderReplyKind.Transient, null, statusCode, reason, retryAfter);

    public static ProviderReply Rejected(string reason, int? statusCode = null) =>
        new(ProviderReplyKind.Rejected, null, statusCode, reason, null);
}
