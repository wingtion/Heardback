namespace Heardback.Infrastructure.Llm.Providers;

internal enum ProviderReplyKind
{
    Answered,

    /// <summary>Rate limit, server error, timeout or broken response: the next provider may do better.</summary>
    Transient,

    /// <summary>The request itself was refused; another provider would most likely refuse it too.</summary>
    Rejected,
}
