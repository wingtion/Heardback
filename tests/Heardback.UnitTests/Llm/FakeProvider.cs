using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Providers;

namespace Heardback.UnitTests.Llm;

internal sealed class FakeProvider(
    string name,
    ProviderReply reply,
    bool isConfigured = true,
    bool allowsPersonalData = true) : ILlmProvider
{
    public string Name => name;

    public string Model => $"{name}-model";

    public bool IsConfigured => isConfigured;

    public bool AllowsPersonalData => allowsPersonalData;

    public int Calls { get; private set; }

    public static ProviderReply AnswerFrom(string provider) =>
        ProviderReply.Answered(new LlmCompletion(provider, $"{provider}-model", """{"title":"x"}""", 10, 5));

    public Task<ProviderReply> SendAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(reply);
    }
}
